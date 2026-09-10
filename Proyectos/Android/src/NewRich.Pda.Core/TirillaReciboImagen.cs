using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Services;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using Point = SixLabors.ImageSharp.Point;
using PointF = SixLabors.ImageSharp.PointF;

namespace NewRich.Pda.Core;

public static class TirillaReciboImagen
{
    private const int Ancho = 420;
    private const int Margen = 16;
    private static readonly object Candado = new();
    private static FontFamily? _familia;

    public static byte[] Jpeg(TirillaResponse tirilla, out int ancho, out int alto)
    {
        var combinada = TirillaCuerpo.EsCombinada(tirilla.TipoApuesta);
        var fecha = tirilla.Fecha.Kind == DateTimeKind.Utc ? tirilla.Fecha.ToLocalTime() : tirilla.Fecha;
        var leyenda = TirillaCuerpo.Leyenda(tirilla.VigenciaDias > 0 ? tirilla.VigenciaDias : 30);
        var qr = QrImagen.Png(tirilla.Qr);
        var filasJuego = combinada ? 2 : Math.Max(1, tirilla.Juegos.Count);
        alto = 260 + (filasJuego * 22) + (qr.Length > 0 ? 150 : 0) + 110;
        ancho = Ancho;
        var tinta = Color.FromRgb(23, 33, 43);
        var regular = new Font(Familia(), 13);
        var negrita = new Font(Familia(), 13, FontStyle.Bold);
        var pequena = new Font(Familia(), 12);

        using var imagen = new Image<Rgba32>(ancho, alto);
        imagen.Mutate(ctx =>
        {
            ctx.Fill(Color.FromRgb(251, 253, 252));
            float y = Margen;
            void Regla()
            {
                ctx.DrawText(new RichTextOptions(regular) { Origin = new PointF(Margen, y) }, "================================", tinta);
                y += 18;
            }

            void Fila(string izquierda, string derecha, bool gruesa = false)
            {
                var fuenteIzq = gruesa ? negrita : regular;
                ctx.DrawText(new RichTextOptions(fuenteIzq) { Origin = new PointF(Margen, y) }, izquierda, tinta);
                var med = TextMeasurer.MeasureSize(derecha, new TextOptions(negrita));
                ctx.DrawText(new RichTextOptions(negrita) { Origin = new PointF(Ancho - Margen - med.Width, y) }, derecha, tinta);
                y += 18;
            }

            void Tres(string a, string b, string c, bool gruesa = false)
            {
                var fuente = gruesa ? negrita : regular;
                var col = (Ancho - Margen * 2) / 3f;
                ctx.DrawText(new RichTextOptions(fuente) { Origin = new PointF(Margen, y) }, a, tinta);
                var medB = TextMeasurer.MeasureSize(b, new TextOptions(fuente));
                ctx.DrawText(new RichTextOptions(fuente) { Origin = new PointF(Margen + col + (col - medB.Width) / 2, y) }, b, tinta);
                var medC = TextMeasurer.MeasureSize(c, new TextOptions(fuente));
                ctx.DrawText(new RichTextOptions(fuente) { Origin = new PointF(Ancho - Margen - medC.Width, y) }, c, tinta);
                y += 18;
            }

            Regla();
            Fila("RECIBO DE VENTA", tirilla.CodigoImpreso);
            Regla();
            Fila($"Fecha: {fecha:yyyy-MM-dd}", $"Hora: {fecha:HH:mm}");
            Fila("Tipo de apuesta:", TirillaCuerpo.EtiquetaTipo(tirilla.TipoApuesta));
            Regla();
            var jugado = "JUGADO";
            var medJ = TextMeasurer.MeasureSize(jugado, new TextOptions(negrita));
            ctx.DrawText(new RichTextOptions(negrita) { Origin = new PointF((Ancho - medJ.Width) / 2, y) }, jugado, tinta);
            y += 18;
            Regla();
            if (combinada && tirilla.Juegos.Count > 0)
            {
                var juego = tirilla.Juegos[0];
                Tres("NUMERO", "VALOR", "TOTAL", true);
                Tres(juego.Numero, TirillaCuerpo.Pesos(juego.Valor), TirillaCuerpo.Pesos(juego.Total));
                ctx.DrawText(new RichTextOptions(regular) { Origin = new PointF(Margen, y), WrappingLength = Ancho - Margen * 2 },
                    "LOTERIAS: " + string.Join(", ", juego.Loterias).ToUpperInvariant(), tinta);
                y += 36;
            }
            else
            {
                Tres("NUMERO", "VALOR", "LOTERIA", true);
                foreach (var juego in tirilla.Juegos)
                {
                    var loteria = (juego.Loterias.FirstOrDefault() ?? string.Empty).ToUpperInvariant();
                    Tres(juego.Numero, TirillaCuerpo.Pesos(juego.Valor), loteria);
                }
            }

            Regla();
            Fila("TOTAL APOSTADO", TirillaCuerpo.Pesos(tirilla.Total), true);
            Regla();
            if (qr.Length > 0)
            {
                using var qrImg = Image.Load<Rgba32>(qr);
                qrImg.Mutate(q => q.Resize(128, 128));
                ctx.DrawImage(qrImg, new Point((Ancho - 128) / 2, (int)y), 1f);
                y += 140;
            }

            Regla();
            ctx.DrawText(new RichTextOptions(pequena)
            {
                Origin = new PointF(Margen, y),
                WrappingLength = Ancho - Margen * 2
            }, leyenda, tinta);
        });

        using var salida = new MemoryStream();
        imagen.SaveAsJpeg(salida, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 90 });
        return salida.ToArray();
    }

    private static FontFamily Familia()
    {
        lock (Candado)
        {
            if (_familia.HasValue)
            {
                return _familia.Value;
            }

            var ensamblado = typeof(TirillaDocumentoPdf).Assembly;
            var recurso = ensamblado.GetManifestResourceNames()
                .First(n => n.EndsWith("OpenSans-Regular.ttf", StringComparison.OrdinalIgnoreCase));
            using var origen = ensamblado.GetManifestResourceStream(recurso)
                ?? throw new InvalidOperationException("No se encontro la fuente de la tirilla.");
            var coleccion = new FontCollection();
            _familia = coleccion.Add(origen);
            return _familia.Value;
        }
    }
}
