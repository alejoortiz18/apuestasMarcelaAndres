using NewRich.Application.Contracts.Boletos;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Observador;

public sealed class ObservadorHomePage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly SesionPda _sesion;
    private readonly Entry _qr = Ui.Entrada(string.Empty);
    private readonly Label _resultado = new() { FontSize = 13, TextColor = Ui.Ink };

    public ObservadorHomePage(NewRichApiClient api, SesionPda sesion)
    {
        _api = api;
        _sesion = sesion;
        Title = PdaTexts.Inicio;
        BackgroundColor = Ui.Paper;
        var escanear = Ui.Primario(PdaTexts.EscanearCamara);
        escanear.Clicked += async (_, _) => await this.AvisoAsync(PdaTexts.ValidarBoleto, "El lector de cámara QR se habilitará cuando se defina el paquete de lectura. Use el código cifrado o el payload del QR en el campo manual.", PdaTexts.Cerrar);
        var validar = Ui.Secundario(PdaTexts.IngresarCodigoManual);
        validar.Clicked += async (_, _) => await ValidarAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 12,
                Children =
                {
                    new Frame
                    {
                        BackgroundColor = Ui.Dark,
                        BorderColor = Ui.Dark,
                        Padding = 18,
                        Content = new VerticalStackLayout
                        {
                            Children =
                            {
                                new Label { Text = PdaTexts.Marca, TextColor = Color.FromArgb("#aed8c4"), FontAttributes = FontAttributes.Bold },
                                Ui.Titulo(_sesion.Usuario?.NombreCompleto ?? string.Empty),
                                new Label
                                {
                                    Text = string.IsNullOrWhiteSpace(_sesion.Usuario?.GrupoNombre) ? PdaTexts.GrupoNoConsultado : _sesion.Usuario!.GrupoNombre,
                                    TextColor = Color.FromArgb("#bce9cc"),
                                    FontSize = 13
                                },
                                new Label { Text = PdaTexts.PdaObservador, TextColor = Color.FromArgb("#bce9cc"), FontSize = 12 }
                            }
                        }
                    },
                    new Label { Text = PdaTexts.ValidarBoleto, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                    Ui.Campo("QR o payload"),
                    _qr,
                    escanear,
                    validar,
                    _resultado
                }
            }
        };
    }

    private async Task ValidarAsync()
    {
        var resultado = await _api.ValidarQrAsync(new ValidarQrRequest { Qr = _qr.Text?.Trim() ?? string.Empty }, CancellationToken.None);
        if (!resultado.IsSuccess || resultado.Data is null)
        {
            _resultado.Text = resultado.Message;
            _resultado.TextColor = Ui.Danger;
            return;
        }

        var d = resultado.Data;
        _resultado.Text = $"{d.ResultadoVisual}\n{d.CodigoPublico} · {d.Vendedor} · {d.Estado} · {d.Vigencia}";
        _resultado.TextColor = Ui.Green;
    }
}
