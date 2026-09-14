using NewRich.Pda.Core;

namespace NewRich.Maui.Views.Observador;

public sealed class ComprobanteEntregaPage : ContentPage
{
    private readonly ComprobanteEntregaDatos _datos;
    private readonly IReadOnlyList<(string Titulo, byte[] Contenido)> _evidencias;

    public ComprobanteEntregaPage(ComprobanteEntregaDatos datos, IReadOnlyList<(string Titulo, byte[] Contenido)> evidencias)
    {
        _datos = datos;
        _evidencias = evidencias;
        Title = PdaTexts.ComprobanteTitulo;
        BackgroundColor = Ui.Paper;

        var compartir = Ui.Primario(PdaTexts.ComprobanteCompartir);
        compartir.Clicked += async (_, _) => await CompartirAsync();
        var volver = Ui.Secundario(PdaTexts.ComprobanteVolver);
        volver.Clicked += async (_, _) => await Navigation.PopToRootAsync();

        var cuerpo = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 14,
            Children =
            {
                Ui.Banner($"{PdaTexts.ComprobantePremioEntregado} · {PdaTexts.ComprobanteTicket} {_datos.Ticket}", Ui.Mint, Ui.Green),
                Ui.Tarjeta(new VerticalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        Dato(PdaTexts.ComprobanteGanador, ComprobanteEntrega.NombreCompleto(_datos)),
                        Dato(PdaTexts.NumeroContacto, _datos.NumeroContacto),
                        Dato(PdaTexts.LugarGano, _datos.LugarGano),
                        Dato(PdaTexts.NombreVendedor, _datos.NombreVendedor),
                        Dato(PdaTexts.ValorTotalGanado, FormatoDinero.Pesos(_datos.ValorTotalGanado)),
                        Dato(PdaTexts.PersonaQueEntrega, _datos.PersonaQueEntrega),
                        Dato(PdaTexts.ComprobanteFecha, _datos.FechaEntrega.ToString("dd/MM/yyyy HH:mm"))
                    }
                }),
                new Label
                {
                    Text = PdaTexts.ComprobanteEvidencias,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Ui.Ink
                }
            }
        };

        foreach (var evidencia in _evidencias)
        {
            cuerpo.Children.Add(Evidencia(evidencia.Titulo, evidencia.Contenido));
        }

        cuerpo.Children.Add(compartir);
        cuerpo.Children.Add(volver);
        Content = new ScrollView { Content = cuerpo };
    }

    private async Task CompartirAsync()
    {
        try
        {
            var resumen = Path.Combine(FileSystem.CacheDirectory, $"comprobante-{_datos.Ticket}.txt");
            await File.WriteAllTextAsync(resumen, ComprobanteEntrega.Texto(_datos));
            var archivos = new List<ShareFile> { new(resumen) };
            var indice = 1;
            foreach (var evidencia in _evidencias)
            {
                var ruta = Path.Combine(FileSystem.CacheDirectory, $"comprobante-{_datos.Ticket}-{indice}.jpg");
                await File.WriteAllBytesAsync(ruta, evidencia.Contenido);
                archivos.Add(new ShareFile(ruta));
                indice++;
            }

            await Share.Default.RequestAsync(new ShareMultipleFilesRequest
            {
                Title = PdaTexts.ComprobanteTitulo,
                Files = archivos
            });
        }
        catch (Exception)
        {
            await this.AvisoAsync(PdaTexts.ComprobanteTitulo, PdaTexts.ComprobanteNoCompartido, PdaTexts.Cerrar);
        }
    }

    private static View Dato(string titulo, string valor) => new VerticalStackLayout
    {
        Spacing = 2,
        Children =
        {
            new Label { Text = titulo, FontSize = 11, TextColor = Ui.Muted },
            new Label
            {
                Text = string.IsNullOrWhiteSpace(valor) ? "-" : valor,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Ui.Ink
            }
        }
    };

    private static View Evidencia(string titulo, byte[] contenido) => Ui.Tarjeta(new VerticalStackLayout
    {
        Spacing = 8,
        Children =
        {
            new Label { Text = titulo, FontSize = 12, TextColor = Ui.Muted },
            new Image
            {
                Source = ImageSource.FromStream(() => new MemoryStream(contenido)),
                Aspect = Aspect.AspectFit,
                HeightRequest = 180
            }
        }
    });
}
