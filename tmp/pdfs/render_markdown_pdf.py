from __future__ import annotations

import html
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import cm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    KeepTogether,
    Image,
    PageBreak,
    Paragraph,
    Preformatted,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)

PAGE_WIDTH, PAGE_HEIGHT = A4
MARGIN = 1.55 * cm
CONTENT_WIDTH = PAGE_WIDTH - (MARGIN * 2)


def register_fonts() -> tuple[str, str]:
    regular = Path(r"C:\Windows\Fonts\arial.ttf")
    bold = Path(r"C:\Windows\Fonts\arialbd.ttf")
    if regular.exists() and bold.exists():
        pdfmetrics.registerFont(TTFont("DocArial", str(regular)))
        pdfmetrics.registerFont(TTFont("DocArial-Bold", str(bold)))
        return "DocArial", "DocArial-Bold"
    return "Helvetica", "Helvetica-Bold"


REGULAR, BOLD = register_fonts()


def normalize(text: str) -> str:
    replacements = {
        "✅": "Sí", "❌": "No", "🔔": "Notificaciones", "🟢": "",
        "🔴": "", "→": "->", "─": "-", "│": "|", "┌": "+",
        "┐": "+", "└": "+", "┘": "+", "├": "+", "┤": "+",
        "┬": "+", "┴": "+", "┼": "+", "▼": "v", "►": ">",
        "✓": "[x]",
    }
    for old, new in replacements.items():
        text = text.replace(old, new)
    return text


def rich(text: str) -> str:
    text = normalize(text)
    text = html.escape(text)
    text = re.sub(r"`([^`]+)`", r'<font name="Courier">\1</font>', text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"<b>\1</b>", text)
    text = re.sub(r"\*([^*]+)\*", r"<i>\1</i>", text)
    return text.replace("  ", "&nbsp; ")


def table_cells(line: str) -> list[str]:
    raw = line.strip().strip("|")
    return [part.strip().replace(r"\|", "|") for part in raw.split("|")]


def footer(canvas, doc):
    canvas.saveState()
    canvas.setStrokeColor(colors.HexColor("#D1D5DB"))
    canvas.line(MARGIN, 1.2 * cm, PAGE_WIDTH - MARGIN, 1.2 * cm)
    canvas.setFont(REGULAR, 8)
    canvas.setFillColor(colors.HexColor("#64748B"))
    canvas.drawString(MARGIN, 0.75 * cm, "New Rich")
    canvas.drawRightString(PAGE_WIDTH - MARGIN, 0.75 * cm, f"Página {doc.page}")
    canvas.restoreState()


def styles():
    base = getSampleStyleSheet()
    return {
        "title": ParagraphStyle("Title", parent=base["Title"], fontName=BOLD, fontSize=20,
                                leading=25, textColor=colors.HexColor("#0F172A"), spaceAfter=12),
        "h1": ParagraphStyle("H1", parent=base["Heading1"], fontName=BOLD, fontSize=15,
                             leading=19, textColor=colors.HexColor("#123B5D"), spaceBefore=14, spaceAfter=8),
        "h2": ParagraphStyle("H2", parent=base["Heading2"], fontName=BOLD, fontSize=12,
                             leading=15, textColor=colors.HexColor("#1D4E78"), spaceBefore=11, spaceAfter=6),
        "h3": ParagraphStyle("H3", parent=base["Heading3"], fontName=BOLD, fontSize=10.5,
                             leading=13, textColor=colors.HexColor("#334155"), spaceBefore=8, spaceAfter=4),
        "body": ParagraphStyle("Body", parent=base["BodyText"], fontName=REGULAR, fontSize=9,
                               leading=12.2, textColor=colors.HexColor("#1F2937"), spaceAfter=5),
        "bullet": ParagraphStyle("Bullet", parent=base["BodyText"], fontName=REGULAR, fontSize=9,
                                 leading=12, leftIndent=13, firstLineIndent=-8, spaceAfter=3),
        "quote": ParagraphStyle("Quote", parent=base["BodyText"], fontName=REGULAR, fontSize=9,
                                leading=12, leftIndent=10, borderColor=colors.HexColor("#93C5FD"),
                                borderWidth=1, borderPadding=6, backColor=colors.HexColor("#EFF6FF"), spaceAfter=8),
        "table": ParagraphStyle("Table", parent=base["BodyText"], fontName=REGULAR, fontSize=7.2,
                                leading=9.2, textColor=colors.HexColor("#1F2937")),
        "table_head": ParagraphStyle("TableHead", parent=base["BodyText"], fontName=BOLD, fontSize=7.2,
                                     leading=9.2, textColor=colors.white),
    }


def mermaid_command() -> list[str]:
    configured = shutil.which("mmdc") or shutil.which("mmdc.cmd")
    if configured:
        return [configured]

    npx = shutil.which("npx") or shutil.which("npx.cmd")
    if npx:
        return [npx, "--yes", "-p", "@mermaid-js/mermaid-cli", "mmdc"]

    raise RuntimeError(
        "No se encontró Mermaid CLI. Instala Node.js y ejecuta "
        "'npm install -g @mermaid-js/mermaid-cli', o define el ejecutable mmdc en PATH."
    )


def render_mermaid(source: str, workdir: Path) -> Path:
    input_file = workdir / f"diagram_{len(list(workdir.glob('diagram_*.mmd'))):04d}.mmd"
    output_file = input_file.with_suffix(".png")
    input_file.write_text(source, encoding="utf-8")
    command = mermaid_command() + [
        "-i", str(input_file),
        "-o", str(output_file),
        "-b", "white",
        "-s", "1",
    ]
    result = subprocess.run(command, capture_output=True, text=True)
    if result.returncode != 0 or not output_file.exists():
        details = (result.stderr or result.stdout).strip()
        raise RuntimeError(f"No se pudo renderizar un diagrama Mermaid: {details}")
    return output_file


def markdown_to_story(text: str, workdir: Path):
    style = styles()
    story = []
    lines = text.splitlines()
    index = 0
    in_code = False
    code_lang = ""
    code = []

    while index < len(lines):
        line = lines[index]
        if line.startswith("```"):
            if not in_code:
                in_code = True
                code_lang = line[3:].strip()
                code = []
            else:
                if code_lang == "mermaid":
                    image_file = render_mermaid("\n".join(code), workdir)
                    story.append(Paragraph("Diagrama de flujo", style["h3"]))
                    story.append(Image(str(image_file), width=CONTENT_WIDTH, height=CONTENT_WIDTH * 0.56))
                else:
                    block = [Paragraph("Detalle técnico", style["h3"]),
                             Preformatted(normalize("\n".join(code)), ParagraphStyle("Code", fontName="Courier", fontSize=6.8, leading=8.2, textColor=colors.HexColor("#0F172A")))]
                    boxed = Table([[block]], colWidths=[CONTENT_WIDTH])
                    boxed.setStyle(TableStyle([
                        ("BACKGROUND", (0, 0), (-1, -1), colors.HexColor("#F8FAFC")),
                        ("BOX", (0, 0), (-1, -1), 0.6, colors.HexColor("#94A3B8")),
                        ("LEFTPADDING", (0, 0), (-1, -1), 8),
                        ("RIGHTPADDING", (0, 0), (-1, -1), 8),
                        ("TOPPADDING", (0, 0), (-1, -1), 6),
                        ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
                    ]))
                    story.append(boxed)
                story.append(Spacer(1, 6))
                in_code = False
            index += 1
            continue
        if in_code:
            code.append(line)
            index += 1
            continue
        if not line.strip():
            index += 1
            continue
        if line.startswith("|") and index + 1 < len(lines) and re.match(r"^\|\s*[-:]+", lines[index + 1]):
            rows = [table_cells(line)]
            index += 2
            while index < len(lines) and lines[index].startswith("|"):
                rows.append(table_cells(lines[index]))
                index += 1
            columns = max(len(row) for row in rows)
            rows = [row + [""] * (columns - len(row)) for row in rows]
            widths = [CONTENT_WIDTH / columns] * columns
            data = []
            for row_index, row in enumerate(rows):
                row_style = style["table_head"] if row_index == 0 else style["table"]
                data.append([Paragraph(rich(cell), row_style) for cell in row])
            table = Table(data, colWidths=widths, repeatRows=1, hAlign="LEFT")
            table.setStyle(TableStyle([
                ("BACKGROUND", (0, 0), (-1, 0), colors.HexColor("#1D4E78")),
                ("GRID", (0, 0), (-1, -1), 0.35, colors.HexColor("#CBD5E1")),
                ("VALIGN", (0, 0), (-1, -1), "TOP"),
                ("LEFTPADDING", (0, 0), (-1, -1), 5),
                ("RIGHTPADDING", (0, 0), (-1, -1), 5),
                ("TOPPADDING", (0, 0), (-1, -1), 4),
                ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
                ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, colors.HexColor("#F8FAFC")]),
            ]))
            story.extend([table, Spacer(1, 8)])
            continue
        if line.startswith("# "):
            story.append(Paragraph(rich(line[2:]), style["title"]))
        elif line.startswith("## "):
            story.append(Paragraph(rich(line[3:]), style["h1"]))
        elif line.startswith("### "):
            story.append(Paragraph(rich(line[4:]), style["h2"]))
        elif line.startswith("#### "):
            story.append(Paragraph(rich(line[5:]), style["h3"]))
        elif line.strip() == "---":
            story.append(Spacer(1, 8))
        elif line.startswith("> "):
            story.append(Paragraph(rich(line[2:]), style["quote"]))
        elif re.match(r"^[-*]\s+", line):
            story.append(Paragraph("• " + rich(re.sub(r"^[-*]\s+", "", line)), style["bullet"]))
        elif re.match(r"^\d+\.\s+", line):
            story.append(Paragraph(rich(line), style["bullet"]))
        else:
            story.append(Paragraph(rich(line), style["body"]))
        index += 1
    return story


def render(source: Path, destination: Path):
    document = SimpleDocTemplate(
        str(destination), pagesize=A4, rightMargin=MARGIN, leftMargin=MARGIN,
        topMargin=1.6 * cm, bottomMargin=1.7 * cm, title=source.stem,
        author="Sistema de Gestión de Apuestas",
    )
    with tempfile.TemporaryDirectory(prefix="new_rich_mermaid_") as temporary:
        story = markdown_to_story(source.read_text(encoding="utf-8"), Path(temporary))
        document.build(story, onFirstPage=footer, onLaterPages=footer)


if __name__ == "__main__":
    documents = Path(r"D:\Desarrollo\chances\SWApuestas\documentos")

    files = [
        ("REQUERIMIENTOS DE SOFTWARE.MD", "REQUERIMIENTOS DE SOFTWARE.pdf"),
        ("historiasUsuarioApuestas.md", "historiasUsuarioApuestas.pdf"),
        ("requerimientoApuestas.md", "requerimientoApuestas.pdf"),
        ("diagrama.md", "diagrama.pdf"),
    ]

    for input_name, output_name in files:
        input_file = documents / input_name
        output_file = documents / output_name

        if not input_file.exists():
            print(f"No se encontró: {input_file}")
            continue

        print(f"Convirtiendo: {input_file.name}")
        render(input_file, output_file)
        print(f"Generado: {output_file}")
