#!/usr/bin/env python3
"""
Valida y deja operativo el entorno local: SQL Server, la API NewRich y el Administrador.

Revisa en orden: herramientas, base de datos, configuración de Development,
procesos y puertos, y por último que cada aplicación responda por HTTP.
Lo que esté caído se levanta; lo que esté mal configurado se reporta sin tocar
los archivos de producción.

Uso (desde la raíz del repositorio):
    python validarServicios.py              valida y levanta lo que falte
    python validarServicios.py --reiniciar  baja y vuelve a levantar todo
    python validarServicios.py --abrir      además abre el Administrador en el navegador
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import time
import urllib.error
import urllib.request
import webbrowser
from pathlib import Path

RAIZ = Path(__file__).resolve().parent
DOTNET = "dotnet"

API_PROYECTO = RAIZ / "Proyectos" / "API" / "src" / "NewRich.Api" / "NewRich.Api.csproj"
ADMIN_PROYECTO = RAIZ / "Proyectos" / "Administrador" / "NewRich.Admin.csproj"
API_AJUSTES = API_PROYECTO.parent / "appsettings.Development.json"
ADMIN_AJUSTES = ADMIN_PROYECTO.parent / "appsettings.Development.json"

API_PUERTO = 5295
ADMIN_PUERTO = 5274
API_URL = f"http://0.0.0.0:{API_PUERTO}"
ADMIN_URL = f"http://0.0.0.0:{ADMIN_PUERTO}"
API_CHEQUEO = f"http://localhost:{API_PUERTO}/api/Premios"
ADMIN_CHEQUEO = f"http://localhost:{ADMIN_PUERTO}/Cuenta/Ingresar"
ADMIN_INICIO = f"http://localhost:{ADMIN_PUERTO}/"

SERVIDOR_SQL = "DESKTOP-JLLF9LK"
BASE_DATOS = "NewRich"
LOG_DIR = RAIZ / "tmp" / "servicios"

ESPERA_ARRANQUE_SEG = 120
INTERVALO_CHEQUEO_SEG = 2
TIEMPO_HTTP_SEG = 15

fallas: list[str] = []


def escribir(mensaje: str = "") -> None:
    print(mensaje, flush=True)


def titulo(texto: str) -> None:
    escribir()
    escribir(f"=== {texto} ===")


def ok(texto: str) -> None:
    escribir(f"  [ok]    {texto}")


def aviso(texto: str) -> None:
    escribir(f"  [aviso] {texto}")


def falla(texto: str, remedio: str = "") -> None:
    escribir(f"  [falla] {texto}")
    if remedio:
        escribir(f"          {remedio}")
    fallas.append(texto)


def ejecutar(comando: list[str], tiempo: int = 60) -> subprocess.CompletedProcess[str]:
    try:
        return subprocess.run(
            comando,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=tiempo,
            check=False,
        )
    except FileNotFoundError:
        return subprocess.CompletedProcess(comando, 9009, "", "programa no encontrado")
    except subprocess.TimeoutExpired:
        return subprocess.CompletedProcess(comando, 1460, "", "se agotó el tiempo de espera")


def powershell(script: str, tiempo: int = 60) -> str:
    resultado = ejecutar(["powershell", "-NoProfile", "-Command", script], tiempo)
    return (resultado.stdout or "").strip()


# --------------------------------------------------------------------------
# 1. Herramientas
# --------------------------------------------------------------------------

def validar_herramientas() -> None:
    titulo("Herramientas")

    version = ejecutar([DOTNET, "--version"], 60)
    if version.returncode == 0:
        ok(f"SDK de .NET {version.stdout.strip()}")
    else:
        falla("No se encontró el SDK de .NET", "Instale .NET y reabra la terminal.")

    for proyecto in (API_PROYECTO, ADMIN_PROYECTO):
        if proyecto.exists():
            ok(f"Proyecto {proyecto.name}")
        else:
            falla(f"No existe el proyecto {proyecto}")


# --------------------------------------------------------------------------
# 2. Base de datos
# --------------------------------------------------------------------------

def consultar_sql(consulta: str) -> tuple[bool, str]:
    resultado = ejecutar(
        [
            "sqlcmd",
            "-S",
            "localhost",
            "-d",
            BASE_DATOS,
            "-E",
            "-C",
            "-I",
            "-h",
            "-1",
            "-W",
            "-Q",
            f"SET NOCOUNT ON; {consulta}",
        ],
        60,
    )
    salida = ((resultado.stdout or "") + (resultado.stderr or "")).strip()
    return resultado.returncode == 0 and "Msg " not in salida, salida


def validar_base_datos() -> None:
    titulo("SQL Server")

    estado = powershell("(Get-Service MSSQLSERVER -ErrorAction SilentlyContinue).Status", 30)
    if estado.lower() != "running":
        aviso(f"MSSQLSERVER está en estado '{estado or 'desconocido'}'. Intentando iniciarlo...")
        ejecutar(["net", "start", "MSSQLSERVER"], 120)
        estado = powershell("(Get-Service MSSQLSERVER -ErrorAction SilentlyContinue).Status", 30)

    if estado.lower() == "running":
        ok("Servicio MSSQLSERVER en ejecución")
    else:
        falla(
            "SQL Server no está en ejecución",
            "Abra PowerShell como administrador y ejecute: net start MSSQLSERVER",
        )
        return

    bien, salida = consultar_sql("SELECT CONVERT(varchar(128), @@SERVERNAME);")
    if not bien:
        falla(f"No se pudo consultar la base {BASE_DATOS}", salida.splitlines()[0] if salida else "")
        return

    servidor = salida.splitlines()[0].strip()
    if servidor.upper() == SERVIDOR_SQL.upper():
        ok(f"Conectado a {servidor}, base {BASE_DATOS}")
    else:
        falla(
            f"La base responde como '{servidor}' y se esperaba '{SERVIDOR_SQL}'",
            "El entorno local debe apuntar a la instancia local, nunca a producción.",
        )
        return

    bien, salida = consultar_sql(
        "SELECT COUNT(*) FROM sys.schemas WHERE name = 'recaudo';"
    )
    if bien and salida.strip().splitlines()[0].strip() == "1":
        ok("Esquema recaudo presente")
    else:
        aviso("Falta el esquema recaudo; aplique Proyectos/BD/Recaudo_Esquema.sql")

    bien, salida = consultar_sql("SELECT COUNT(*) FROM dbo.Usuarios;")
    if bien:
        ok(f"Usuarios en la base: {salida.strip().splitlines()[0].strip()}")


# --------------------------------------------------------------------------
# 3. Configuración de Development
# --------------------------------------------------------------------------

def leer_json(ruta: Path) -> dict:
    try:
        return json.loads(ruta.read_text(encoding="utf-8-sig"))
    except Exception as error:  # noqa: BLE001
        falla(f"No se pudo leer {ruta.name}", str(error))
        return {}


def validar_configuracion() -> None:
    titulo("Configuración de Development")

    api = leer_json(API_AJUSTES)
    cadena = api.get("ConnectionStrings", {}).get("NewRichDatabase", "")
    if "localhost" in cadena.lower() and BASE_DATOS.lower() in cadena.lower():
        ok("La API apunta a la base local NewRich")
    else:
        falla(
            "La API no apunta a la base local",
            f"Revise ConnectionStrings:NewRichDatabase en {API_AJUSTES}",
        )

    admin = leer_json(ADMIN_AJUSTES)
    base = admin.get("Api", {}).get("BaseUrl", "")
    if base.startswith(f"http://localhost:{API_PUERTO}"):
        ok(f"El Administrador apunta a la API local ({base})")
    else:
        falla(
            "El Administrador no apunta a la API local",
            f"Revise Api:BaseUrl en {ADMIN_AJUSTES}",
        )


# --------------------------------------------------------------------------
# 4. Procesos y arranque
# --------------------------------------------------------------------------

def pids_en_puerto(puerto: int) -> list[int]:
    salida = powershell(
        f"Get-NetTCPConnection -State Listen -LocalPort {puerto} -ErrorAction SilentlyContinue | "
        "Select-Object -ExpandProperty OwningProcess",
        60,
    )
    return sorted({int(l) for l in salida.splitlines() if l.strip().isdigit()})


def detener(pids: list[int], puerto: int) -> None:
    for pid in pids:
        ejecutar(["taskkill", "/PID", str(pid), "/T", "/F"], 30)
    for _ in range(20):
        if not pids_en_puerto(puerto):
            return
        time.sleep(0.5)


def http_responde(url: str) -> tuple[int | None, str]:
    try:
        with urllib.request.urlopen(url, timeout=TIEMPO_HTTP_SEG) as respuesta:
            return respuesta.status, ""
    except urllib.error.HTTPError as error:
        # 401 en la API significa que está viva y exige autenticación.
        return error.code, ""
    except Exception as error:  # noqa: BLE001
        return None, str(error)


def compilar(nombre: str, proyecto: Path) -> bool:
    resultado = ejecutar([DOTNET, "build", str(proyecto), "-c", "Debug", "-v", "m"], 600)
    if resultado.returncode == 0:
        ok(f"{nombre} compila sin errores")
        return True

    falla(f"{nombre} no compila", "Últimas líneas del compilador:")
    for linea in (resultado.stdout or resultado.stderr or "").splitlines()[-12:]:
        escribir(f"          {linea}")
    return False


def lanzar(nombre: str, proyecto: Path, url: str, log: str) -> None:
    LOG_DIR.mkdir(parents=True, exist_ok=True)
    salida = open(LOG_DIR / f"{log}-out.log", "a", encoding="utf-8")
    errores = open(LOG_DIR / f"{log}-err.log", "a", encoding="utf-8")
    salida.write(f"\n--- arranque {time.strftime('%Y-%m-%d %H:%M:%S')} ---\n")
    salida.flush()

    entorno = dict(os.environ)
    entorno["ASPNETCORE_ENVIRONMENT"] = "Development"
    entorno["DOTNET_ENVIRONMENT"] = "Development"
    subprocess.Popen(
        [DOTNET, "run", "--project", str(proyecto), "--no-build", "--no-launch-profile", "--urls", url],
        cwd=str(RAIZ),
        stdout=salida,
        stderr=errores,
        env=entorno,
        creationflags=subprocess.CREATE_NEW_PROCESS_GROUP | subprocess.DETACHED_PROCESS,
    )
    aviso(f"{nombre} iniciándose en {url} (log: {LOG_DIR / (log + '-out.log')})")


def esperar(nombre: str, chequeo: str, esperados: tuple[int, ...]) -> bool:
    limite = time.time() + ESPERA_ARRANQUE_SEG
    detalle = "sin respuesta"
    while time.time() < limite:
        codigo, error = http_responde(chequeo)
        if codigo in esperados:
            ok(f"{nombre} responde HTTP {codigo} en {chequeo}")
            return True
        detalle = f"HTTP {codigo}" if codigo else error
        time.sleep(INTERVALO_CHEQUEO_SEG)

    falla(f"{nombre} no respondió a tiempo ({detalle})", f"Revise los logs en {LOG_DIR}")
    return False


def asegurar(nombre: str, proyecto: Path, url: str, log: str, chequeo: str,
             puerto: int, esperados: tuple[int, ...], reiniciar: bool) -> bool:
    titulo(nombre)

    ocupantes = pids_en_puerto(puerto)
    if reiniciar and ocupantes:
        aviso(f"Deteniendo procesos en el puerto {puerto}: {', '.join(map(str, ocupantes))}")
        detener(ocupantes, puerto)
        ocupantes = pids_en_puerto(puerto)

    if ocupantes:
        codigo, error = http_responde(chequeo)
        if codigo in esperados:
            ok(f"{nombre} ya estaba arriba y responde HTTP {codigo}")
            return True
        aviso(f"El puerto {puerto} está ocupado pero no responde bien ({error or f'HTTP {codigo}'}). Reiniciando.")
        detener(ocupantes, puerto)

    if not compilar(nombre, proyecto):
        return False

    lanzar(nombre, proyecto, url, log)
    return esperar(nombre, chequeo, esperados)


# --------------------------------------------------------------------------
# 5. Calentamiento
# --------------------------------------------------------------------------

RUTAS_CALENTAR = (
    "/", "/Ventas", "/Loterias", "/Usuarios", "/Grupos", "/Resultados",
    "/Kpi", "/Notificaciones", "/Soporte", "/Configuracion", "/Instalaciones",
    "/ConfigRecaudo", "/PanelRecaudo", "/HistorialRecaudo", "/MetricasRecaudo",
)


def calentar(usuario: str, clave: str) -> None:
    """La primera visita a cada página compila y carga código; recorrerlas aquí
    evita que el primer clic del usuario se sienta lento."""
    titulo("Calentamiento de páginas")

    import http.cookiejar
    import re
    import urllib.parse

    tarro = http.cookiejar.CookieJar()
    navegador = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(tarro))

    try:
        with navegador.open(f"http://localhost:{ADMIN_PUERTO}/Cuenta/Ingresar", timeout=60) as r:
            html = r.read().decode("utf-8", "replace")
        token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', html)
        if not token:
            aviso("No se encontró el token del formulario; se omite el calentamiento.")
            return

        datos = urllib.parse.urlencode({
            "__RequestVerificationToken": token.group(1),
            "Usuario": usuario,
            "Password": clave,
        }).encode()
        inicio = time.time()
        with navegador.open(f"http://localhost:{ADMIN_PUERTO}/Cuenta/Ingresar", datos, timeout=120) as r:
            final = r.geturl()
        if "Ingresar" in final:
            aviso(f"No se pudo ingresar como '{usuario}'; se omite el calentamiento.")
            return
        ok(f"Ingreso de '{usuario}' en {time.time() - inicio:.1f} s")
    except Exception as error:  # noqa: BLE001
        aviso(f"No se pudo calentar: {error}")
        return

    lentas: list[tuple[str, float]] = []
    for ruta in RUTAS_CALENTAR:
        inicio = time.time()
        try:
            with navegador.open(f"http://localhost:{ADMIN_PUERTO}{ruta}", timeout=120) as r:
                r.read()
        except Exception:  # noqa: BLE001
            continue
        tardanza = time.time() - inicio
        if tardanza > 1:
            lentas.append((ruta, tardanza))

    ok(f"{len(RUTAS_CALENTAR)} páginas recorridas")
    for ruta, tardanza in sorted(lentas, key=lambda x: -x[1]):
        aviso(f"{ruta} tardó {tardanza:.1f} s en la primera visita")


# --------------------------------------------------------------------------

def main() -> int:
    analizador = argparse.ArgumentParser(description="Valida y levanta el entorno local de NewRich.")
    analizador.add_argument("--reiniciar", action="store_true", help="Detiene y vuelve a levantar API y Administrador.")
    analizador.add_argument("--abrir", action="store_true", help="Abre el Administrador en el navegador al terminar.")
    analizador.add_argument("--usuario", default=os.environ.get("NEWRICH_USUARIO", ""),
                            help="Usuario para recorrer las páginas y dejarlas calientes.")
    analizador.add_argument("--clave", default=os.environ.get("NEWRICH_CLAVE", ""),
                            help="Contraseña del usuario de calentamiento.")
    opciones = analizador.parse_args()

    escribir(f"Repositorio: {RAIZ}")
    validar_herramientas()
    validar_base_datos()
    validar_configuracion()

    if fallas:
        escribir()
        escribir("No se levantan los servicios porque hay fallas previas:")
        for texto in fallas:
            escribir(f"  - {texto}")
        return 1

    api_ok = asegurar("API", API_PROYECTO, API_URL, "api", API_CHEQUEO,
                      API_PUERTO, (200, 401), opciones.reiniciar)
    admin_ok = False
    if api_ok:
        admin_ok = asegurar("Administrador", ADMIN_PROYECTO, ADMIN_URL, "admin", ADMIN_CHEQUEO,
                            ADMIN_PUERTO, (200,), opciones.reiniciar)
    else:
        aviso("No se levanta el Administrador porque la API no está lista.")

    if admin_ok and opciones.usuario and opciones.clave:
        calentar(opciones.usuario, opciones.clave)

    titulo("Resumen")
    escribir(f"  API            http://localhost:{API_PUERTO}   (401 sin sesión es normal)")
    escribir(f"  Administrador  {ADMIN_INICIO}")
    escribir(f"  Logs           {LOG_DIR}")

    if fallas:
        escribir()
        escribir("Pendientes:")
        for texto in fallas:
            escribir(f"  - {texto}")
        return 1

    if opciones.abrir and admin_ok:
        webbrowser.open(ADMIN_INICIO)

    escribir()
    escribir("Entorno local listo.")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except KeyboardInterrupt:
        escribir("\nCancelado por el usuario.")
        sys.exit(130)
