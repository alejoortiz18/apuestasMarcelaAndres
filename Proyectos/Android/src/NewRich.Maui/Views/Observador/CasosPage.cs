using NewRich.Application.Contracts.Premios;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;

namespace NewRich.Maui.Views.Observador;

public sealed class CasosPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly SesionPda _sesion;
    private readonly VerticalStackLayout _lista = new() { Spacing = 10 };
    private readonly Label _aviso = new() { FontSize = 13, TextColor = Ui.Muted };

    public CasosPage(NewRichApiClient api, SesionPda sesion)
    {
        _api = api;
        _sesion = sesion;
        Title = PdaTexts.CasosPremios;
        BackgroundColor = Ui.Paper;
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Text = PdaTexts.CasosPremiosSeleccionar,
                        FontSize = 13,
                        TextColor = Ui.Muted
                    },
                    _aviso,
                    _lista
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await CargarAsync();
        }
        catch (Exception)
        {
            _aviso.Text = PdaTexts.SinConexionServidor;
            _aviso.TextColor = Ui.Danger;
        }
    }

    private async Task CargarAsync()
    {
        _lista.Children.Clear();
        _aviso.Text = string.Empty;
        var resultado = await _api.PremiosAsignadosAsync(CancellationToken.None);
        if (!resultado.IsSuccess)
        {
            _aviso.Text = resultado.Message;
            _aviso.TextColor = Ui.Danger;
            return;
        }

        var casos = resultado.Data ?? [];
        if (casos.Count == 0)
        {
            _aviso.Text = PdaTexts.SinCasosAsignados;
            _aviso.TextColor = Ui.Muted;
            _lista.Children.Add(Ui.Banner(PdaTexts.SinCasosAsignados, Ui.InfoBg, Ui.Info));
            return;
        }

        foreach (var caso in casos)
        {
            _lista.Children.Add(CrearTarjeta(caso));
        }
    }

    private View CrearTarjeta(CasoGanadorResponse caso)
    {
        var estadoColor = caso.Estado.Equals("En proceso", StringComparison.OrdinalIgnoreCase)
            ? Ui.Warn
            : Ui.Green;

        var abrir = Ui.Primario(PdaTexts.CasosPremiosContinuar);
        abrir.Clicked += async (_, _) => await AbrirRegistroAsync(caso);

        return new Frame
        {
            BackgroundColor = Colors.White,
            BorderColor = Ui.Line,
            CornerRadius = 12,
            Padding = 16,
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label
                    {
                        Text = $"{PdaTexts.ComprobanteTicket} {caso.Ticket}",
                        FontSize = 17,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Ui.Ink
                    },
                    new Label
                    {
                        Text = $"{PdaTexts.CasosPremiosEstado}: {caso.Estado}",
                        FontSize = 13,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = estadoColor
                    },
                    new Label
                    {
                        Text = $"{PdaTexts.CasosPremiosVendedor}: {caso.Vendedor}",
                        FontSize = 13,
                        TextColor = Ui.Muted
                    },
                    abrir
                }
            }
        };
    }

    private async Task AbrirRegistroAsync(CasoGanadorResponse caso)
    {
        try
        {
            await Navigation.PushAsync(new RegistroEntregaPage(_api, _sesion, caso));
        }
        catch (Exception excepcion)
        {
            await this.AvisoAsync(PdaTexts.CasosPremios, excepcion.Message, PdaTexts.Cerrar);
        }
    }
}
