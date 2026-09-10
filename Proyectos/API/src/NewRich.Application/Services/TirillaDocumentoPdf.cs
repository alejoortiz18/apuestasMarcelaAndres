using NewRich.Application.Contracts.Boletos;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace NewRich.Application.Services;

public static class TirillaDocumentoPdf
{
    private static readonly object Candado = new();
    private static bool _fuenteRegistrada;
    private const string Fuente = "TirillaSans";

    public static byte[] Crear(TirillaResponse tirilla)
    {
        AsegurarFuente();
        var combinada = TirillaCuerpo.EsCombinada(tirilla.TipoApuesta);
        var fecha = tirilla.Fecha.Kind == DateTimeKind.Utc ? tirilla.Fecha.ToLocalTime() : tirilla.Fecha;
        var leyenda = TirillaCuerpo.LeyendaDeRespuesta(tirilla.VigenciaDias, tirilla.Leyenda);
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(226, 0);
                page.ContinuousSize(226);
                page.Margin(12);
                page.DefaultTextStyle(t => t.FontFamily(Fuente).FontSize(9).FontColor("#17212b"));
                page.Content().Column(col =>
                {
                    col.Spacing(4);
                    col.Item().Text(Regla);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("RECIBO DE VENTA");
                        r.AutoItem().Text(tirilla.CodigoImpreso).Bold();
                    });
                    col.Item().Text(Regla);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"Fecha: {fecha:yyyy-MM-dd}");
                        r.AutoItem().Text($"Hora: {fecha:HH:mm}").Bold();
                    });
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("Tipo de apuesta:");
                        r.AutoItem().Text(TirillaCuerpo.EtiquetaTipo(tirilla.TipoApuesta)).Bold();
                    });
                    col.Item().Text(Regla);
                    col.Item().AlignCenter().Text("JUGADO").Bold();
                    col.Item().Text(Regla);
                    if (combinada && tirilla.Juegos.Count > 0)
                    {
                        var juego = tirilla.Juegos[0];
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("NUMERO").Bold();
                            r.RelativeItem().AlignCenter().Text("VALOR").Bold();
                            r.RelativeItem().AlignRight().Text("TOTAL").Bold();
                        });
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text(juego.Numero);
                            r.RelativeItem().AlignCenter().Text(TirillaCuerpo.Pesos(juego.Valor));
                            r.RelativeItem().AlignRight().Text(TirillaCuerpo.Pesos(juego.Total));
                        });
                        col.Item().Text("LOTERIAS: " + string.Join(", ", juego.Loterias).ToUpperInvariant());
                    }
                    else
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("NUMERO").Bold();
                            r.RelativeItem().AlignCenter().Text("VALOR").Bold();
                            r.RelativeItem().AlignRight().Text("LOTERIA").Bold();
                        });
                        foreach (var juego in tirilla.Juegos)
                        {
                            var loteria = (juego.Loterias.FirstOrDefault() ?? string.Empty).ToUpperInvariant();
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text(juego.Numero);
                                r.RelativeItem().AlignCenter().Text(TirillaCuerpo.Pesos(juego.Valor));
                                r.RelativeItem().AlignRight().Text(loteria);
                            });
                        }
                    }

                    col.Item().Text(Regla);
                    col.Item().Row(r =>
                    {
                        r.RelativeItem().Text("TOTAL APOSTADO").Bold();
                        r.AutoItem().Text(TirillaCuerpo.Pesos(tirilla.Total)).Bold();
                    });
                    col.Item().Text(Regla);
                    var qrPng = QrImagen.Png(tirilla.Qr);
                    if (qrPng.Length > 0)
                    {
                        col.Item().AlignCenter().Width(96).Image(qrPng);
                    }

                    col.Item().Text(Regla);
                    col.Item().Text(leyenda);
                    col.Item().Text(Regla);
                });
            });
        }).GeneratePdf();
    }

    private const string Regla = "================================";

    private static MemoryStream? _fuente;

    private static void AsegurarFuente()
    {
        lock (Candado)
        {
            if (_fuenteRegistrada)
            {
                return;
            }

            QuestPDF.Settings.License = LicenseType.Community;
            var ensamblado = typeof(TirillaDocumentoPdf).Assembly;
            var recurso = ensamblado.GetManifestResourceNames()
                .First(n => n.EndsWith("OpenSans-Regular.ttf", StringComparison.OrdinalIgnoreCase));
            using var origen = ensamblado.GetManifestResourceStream(recurso)
                ?? throw new InvalidOperationException("No se encontro la fuente de la tirilla.");
            _fuente = new MemoryStream();
            origen.CopyTo(_fuente);
            _fuente.Position = 0;
            QuestPDF.Drawing.FontManager.RegisterFontWithCustomName(Fuente, _fuente);
            _fuenteRegistrada = true;
        }
    }
}
