using Microsoft.Maui.Controls.Shapes;
using NewRich.Constants.Messages;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Services;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Shared;

public sealed class LoginPage : ContentPage
{
    private static readonly Color Fondo = Color.FromArgb("#071410");
    private static readonly Color Tarjeta = Color.FromArgb("#0c1c18");
    private static readonly Color TrazoOro = Color.FromArgb("#c9a44a");
    private static readonly Color BotonOro = Color.FromArgb("#e8c56b");
    private static readonly Color TintaOro = Color.FromArgb("#1a1408");
    private static readonly Color Titulo = Color.FromArgb("#f4efe4");
    private static readonly Color Ayuda = Color.FromArgb("#b8c4b0");
    private static readonly Color Placeholder = Color.FromArgb("#8a9a90");

    private readonly NewRichApiClient _api;
    private readonly ITokenStore _tokens;
    private readonly SesionPda _sesion;
    private readonly NavegadorApp _nav;
    private readonly SincronizacionOfflineServicio _offline;
    private readonly CodigosOfflineEnVivoServicio _enVivo;
    private readonly Entry _usuario;
    private readonly Entry _password;
    private readonly ImageButton _verClave;
    private readonly Button _ingresar;
    private readonly CargandoOverlay _cargando = new();
    private readonly Label _error = new()
    {
        TextColor = Color.FromArgb("#e8a0a0"),
        FontSize = 12,
        Style = null
    };

    public LoginPage(
        NewRichApiClient api,
        ITokenStore tokens,
        SesionPda sesion,
        NavegadorApp nav,
        SincronizacionOfflineServicio offline,
        CodigosOfflineEnVivoServicio enVivo)
    {
        _api = api;
        _tokens = tokens;
        _sesion = sesion;
        _nav = nav;
        _offline = offline;
        _enVivo = enVivo;
        Title = string.Empty;
        NavigationPage.SetHasNavigationBar(this, false);
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Fondo;

        _usuario = CampoTexto(PdaTexts.Usuario);
        _password = CampoTexto(PdaTexts.Contrasena, true);
        _verClave = new ImageButton
        {
            Style = null,
            Source = "login_icon_eye.png",
            BackgroundColor = Colors.Transparent,
            WidthRequest = 36,
            HeightRequest = 36,
            Padding = 4,
            VerticalOptions = LayoutOptions.Center
        };
        SemanticProperties.SetDescription(_verClave, PdaTexts.MostrarContrasena);
        _verClave.Clicked += (_, _) => AlternarClave();

        _ingresar = BotonIngresar();

        var tarjeta = new Border
        {
            Style = null,
            BackgroundColor = Tarjeta,
            Stroke = TrazoOro,
            StrokeThickness = 1.2,
            StrokeShape = new RoundRectangle { CornerRadius = 22 },
            Padding = new Thickness(20, 22),
            Content = new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label
                    {
                        Style = null,
                        Text = PdaTexts.IniciarSesion,
                        FontSize = 26,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Titulo
                    },
                    new Label
                    {
                        Style = null,
                        Text = PdaTexts.LoginAyuda,
                        FontSize = 13,
                        TextColor = Ayuda,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    CampoConIcono("login_icon_user.png", _usuario),
                    CampoConIcono("login_icon_lock.png", _password, _verClave),
                    _ingresar,
                    _error
                }
            }
        };

        Content = new Grid
        {
            BackgroundColor = Fondo,
            Children =
            {
                new Image
                {
                    Source = "login_fondo.png",
                    Aspect = Aspect.AspectFill,
                    InputTransparent = true
                },
                new ScrollView
                {
                    Content = new VerticalStackLayout
                    {
                        Padding = new Thickness(22, 28, 22, 16),
                        Spacing = 16,
                        Children =
                        {
                            new Image
                            {
                                Source = "login_marca.png",
                                Aspect = Aspect.AspectFit,
                                HeightRequest = 92,
                                HorizontalOptions = LayoutOptions.Center
                            },
                            tarjeta,
                            new ContentView { HeightRequest = 168 }
                        }
                    }
                },
                _cargando
            }
        };
    }

    private void AlternarClave()
    {
        _password.IsPassword = !_password.IsPassword;
        SemanticProperties.SetDescription(
            _verClave,
            _password.IsPassword ? PdaTexts.MostrarContrasena : PdaTexts.OcultarContrasena);
    }

    private Button BotonIngresar()
    {
        var boton = new Button
        {
            Style = null,
            Text = PdaTexts.Ingresar,
            BackgroundColor = BotonOro,
            TextColor = TintaOro,
            FontAttributes = FontAttributes.Bold,
            FontSize = 16,
            CornerRadius = 22,
            HeightRequest = 48
        };
        boton.Clicked += async (_, _) => await IngresarAsync();
        return boton;
    }

    private static Entry CampoTexto(string placeholder, bool password = false) => new()
    {
        Style = null,
        Placeholder = placeholder,
        PlaceholderColor = Placeholder,
        IsPassword = password,
        BackgroundColor = Colors.Transparent,
        TextColor = Titulo,
        FontSize = 15,
        VerticalOptions = LayoutOptions.Center,
        ReturnType = ReturnType.Done
    };

    private static Border CampoConIcono(string icono, Entry entrada, View? extra = null)
    {
        var fila = new Grid
        {
            ColumnSpacing = 8,
            HeightRequest = 48,
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            ]
        };

        var imagen = new Image
        {
            Source = icono,
            WidthRequest = 28,
            HeightRequest = 28,
            VerticalOptions = LayoutOptions.Center
        };
        fila.Add(imagen, 0);
        fila.Add(entrada, 1);
        if (extra is not null)
        {
            fila.Add(extra, 2);
        }

        return new Border
        {
            Style = null,
            BackgroundColor = Color.FromArgb("#0a1814"),
            Stroke = TrazoOro,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 24 },
            Padding = new Thickness(12, 4, 8, 4),
            Content = fila
        };
    }

    private async Task IngresarAsync()
    {
        _error.Text = string.Empty;
        _ingresar.IsEnabled = false;
        _cargando.Mostrar(PdaTexts.IniciandoSesion);

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
                await _offline.SincronizarEnSilencioAsync(true, resultado.Data.Rol, false, CancellationToken.None);
                await _enVivo.AsegurarSesionAsync(CancellationToken.None);
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
        finally
        {
            _cargando.Ocultar();
            _ingresar.IsEnabled = true;
        }
    }
}
