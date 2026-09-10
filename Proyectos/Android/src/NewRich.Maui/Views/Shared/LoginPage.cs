using NewRich.Constants.Messages;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Services;

namespace NewRich.Maui.Views.Shared;

public sealed class LoginPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly ITokenStore _tokens;
    private readonly SesionPda _sesion;
    private readonly NavegadorApp _nav;
    private readonly Entry _usuario = Ui.Entrada(string.Empty);
    private readonly Entry _password = Ui.Entrada(string.Empty, true);
    private readonly Label _error = new() { TextColor = Ui.Danger, FontSize = 12 };

    public LoginPage(NewRichApiClient api, ITokenStore tokens, SesionPda sesion, NavegadorApp nav)
    {
        _api = api;
        _tokens = tokens;
        _sesion = sesion;
        _nav = nav;
        Title = PdaTexts.IniciarSesion;
        BackgroundColor = Ui.Paper;

        var ingresar = Ui.Primario(PdaTexts.Ingresar);
        ingresar.Clicked += async (_, _) => await IngresarAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 28,
                Spacing = 12,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    Ui.Tarjeta(new VerticalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            Ui.Marca(PdaTexts.Marca),
                            new Label { Text = PdaTexts.IniciarSesion, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                            new Label { Text = PdaTexts.LoginAyuda, FontSize = 13, TextColor = Ui.Muted },
                            Ui.Campo(PdaTexts.Usuario),
                            _usuario,
                            Ui.Campo(PdaTexts.Contrasena),
                            _password,
                            ingresar,
                            _error
                        }
                    })
                }
            }
        };
    }

    private async Task IngresarAsync()
    {
        _error.Text = string.Empty;

        try
        {
            var conexion = await _api.ConectarAsync(
                PdaConexion.UrlsPara(DeviceInfo.Current.DeviceType == DeviceType.Virtual),
                CancellationToken.None);
            if (!conexion.IsSuccess)
            {
                _error.Text = conexion.Message;
                return;
            }

            var resultado = await _api.LoginAsync(
                PdaConexion.Login(_usuario.Text?.Trim() ?? string.Empty, _password.Text ?? string.Empty),
                CancellationToken.None);

            if (!resultado.IsSuccess || resultado.Data is null)
            {
                _error.Text = AuthPantalla.Mensaje(resultado.Message);
                _sesion.HorarioCerrado = resultado.Message == AuthMessages.FueraDeHorarioOperacion;
                return;
            }

            await _tokens.GuardarAsync(resultado.Data.Token);
            _sesion.Usuario = resultado.Data;
            _sesion.CodigoDispositivo = string.IsNullOrWhiteSpace(resultado.Data.CodigoDispositivo)
                ? PdaConexion.CodigoDispositivo
                : resultado.Data.CodigoDispositivo;
            _sesion.HorarioCerrado = false;

            var operativa = await _api.OperativaAsync(CancellationToken.None);
            if (operativa.IsSuccess && operativa.Data is not null)
            {
                _sesion.Limites = operativa.Data;
            }

            var shell = NavegacionPorRol.Para(resultado.Data.Rol);
            if (!shell.IsSuccess)
            {
                await _tokens.BorrarAsync();
                _error.Text = shell.Message;
                return;
            }

            if (resultado.Data.DebeCambiarPassword)
            {
                _nav.IrAPassword();
                return;
            }

            if (shell.Data == ShellPda.Vendedor)
            {
                _nav.IrAVendedor();
            }
            else
            {
                _nav.IrAObservador();
            }
        }
        catch (HttpRequestException)
        {
            _error.Text = PdaTexts.SinConexionServidor;
        }
        catch (Exception ex)
        {
            _error.Text = ex.Message;
        }
    }
}
