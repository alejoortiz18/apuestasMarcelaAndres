using System.Globalization;
using System.Text;

namespace NewRich.Pda.Core;

public static class JpegEnPdf
{
    public static byte[] Crear(byte[] jpeg, int anchoPx, int altoPx)
    {
        ArgumentNullException.ThrowIfNull(jpeg);
        if (jpeg.Length == 0 || anchoPx <= 0 || altoPx <= 0)
        {
            throw new ArgumentException("La imagen del recibo no es valida.");
        }

        var w = anchoPx.ToString(CultureInfo.InvariantCulture);
        var h = altoPx.ToString(CultureInfo.InvariantCulture);
        var len = jpeg.Length.ToString(CultureInfo.InvariantCulture);
        var obj4 = Encoding.ASCII.GetBytes(
            $"4 0 obj<</Type/XObject/Subtype/Image/Width {w}/Height {h}/ColorSpace/DeviceRGB/BitsPerComponent 8/Filter/DCTDecode/Length {len}>>stream\n");
        var obj4Fin = Encoding.ASCII.GetBytes("\nendstream\nendobj\n");
        var contenido = Encoding.ASCII.GetBytes($"q {w} 0 0 {h} 0 0 cm /Im0 Do Q\n");
        var obj5 = Encoding.ASCII.GetBytes($"5 0 obj<</Length {contenido.Length}>>stream\n");
        var obj5Fin = Encoding.ASCII.GetBytes("endstream\nendobj\n");

        using var pdf = new MemoryStream();
        void Ascii(string texto) => pdf.Write(Encoding.ASCII.GetBytes(texto));

        var offsets = new long[6];
        Ascii("%PDF-1.4\n");
        offsets[1] = pdf.Position;
        Ascii("1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n");
        offsets[2] = pdf.Position;
        Ascii("2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n");
        offsets[3] = pdf.Position;
        Ascii($"3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 {w} {h}]/Resources<</XObject<</Im0 4 0 R>>>>/Contents 5 0 R>>endobj\n");
        offsets[4] = pdf.Position;
        pdf.Write(obj4);
        pdf.Write(jpeg);
        pdf.Write(obj4Fin);
        offsets[5] = pdf.Position;
        pdf.Write(obj5);
        pdf.Write(contenido);
        pdf.Write(obj5Fin);
        var xref = pdf.Position;
        Ascii($"xref\n0 6\n0000000000 65535 f \n");
        for (var i = 1; i <= 5; i++)
        {
            Ascii(offsets[i].ToString("0000000000", CultureInfo.InvariantCulture) + " 00000 n \n");
        }

        Ascii($"trailer<</Size 6/Root 1 0 R>>\nstartxref\n{xref}\n%%EOF\n");
        return pdf.ToArray();
    }
}
