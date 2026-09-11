using NewRich.Constants;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Data;

namespace NewRich.Maui.Views.Vendedor;

public sealed class ConfiguracionPage : ContentPage
{
    private readonly SesionPda _sesion;
    private readonly LocalDatabase _offline;
    private readonly NewRichApiClient _api;
    private readonly Switch _automatica = new();
    private readonly Label _conteo = new() { FontSize = 19, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink };

    public ConfiguracionPage(SesionPda sesion, LocalDatabase offline, NewRichApiClient api)
    {
        _sesion = sesion;
        _offline = offline;
        _api = api;
        Title = PdaTexts.ConfiguracionSync;
        BackgroundColor = Ui.Paper;
        _automatica.IsToggled = string.Equals(_sesion.Limites.SincronizacionModo, ConfiguracionClaves.ModoAutomatica, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Preferences.Default.Get("SyncModo", ConfiguracionClaves.ModoManual), ConfiguracionClaves.ModoAutomatica, StringComparison.OrdinalIgnoreCase);
        _automatica.Toggled += (_, e) =>
        {
            var modo = e.Value ? ConfiguracionClaves.ModoAutomatica : ConfiguracionClaves.ModoManual;
            Preferences.Default.Set("SyncModo", modo);
            _sesion.Limites.SincronizacionModo = modo;
        };

        var descargar = Ui.Primario(PdaTexts.DescargarCodigos);
        descargar.Clicked += async (_, _) =>
        {
            var resultado = await _api.DescargarOfflineAsync(CancellationToken.None);
            if (!resultado.IsSuccess)
            {
                await DisplayAlert(PdaTexts.CodigosOffline, resultado.Message, PdaTexts.Cerrar);
                return;
            }

            if (DescargaCodigosOffline.HayCodigos(resultado.Data))
            {
                await _offline.GuardarDescargaAsync(resultado.Data!.Select(c => (c.Consecutivo, c.PayloadBase64)));
            }

            _conteo.Text = (await _offline.ContarDisponiblesAsync()).ToString();
            await DisplayAlert(PdaTexts.CodigosOffline, DescargaCodigosOffline.Mensaje(resultado.Data), PdaTexts.Cerrar);
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 12,
                Children =
                {
                    Ui.Tarjeta(new HorizontalStackLayout
                    {
                        Spacing = 12,
                        Children =
                        {
                            new VerticalStackLayout
                            {
                                Children =
                                {
                                    new Label { Text = PdaTexts.ModoSincronizacion, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                                    new Label { Text = PdaTexts.SincronizacionAutomatica, FontSize = 11, TextColor = Ui.Muted }
                                }
                            },
                            _automatica
                        }
                    }),
                    Ui.Tarjeta(new VerticalStackLayout
                    {
                        Children =
                        {
                            new Label { Text = PdaTexts.CodigosOffline, FontSize = 11, TextColor = Ui.Muted },
                            _conteo
                        }
                    }),
                    descargar
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _conteo.Text = (await _offline.ContarDisponiblesAsync()).ToString();
    }
}
