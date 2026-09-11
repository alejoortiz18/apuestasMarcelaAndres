using Microsoft.Maui.Controls.Shapes;
using NewRich.Application.Contracts.Auth;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Services;

namespace NewRich.Maui.Views.Shared;

public sealed class PasswordPage : ContentPage
{
    private static readonly Color Fondo = Color.FromArgb("#071410");
    private static readonly Color Tarjeta = Color.FromArgb("#0c1c18");
    private static readonly Color TrazoOro = Color.FromArgb("#c9a44a");
    private static readonly Color BotonOro = Color.FromArgb("#e8c56b");
    private static readonly Color TintaOro = Color.FromArgb("#1a1408");
    private static readonly Color Titulo = Color.FromArgb("#f4efe4");
    private static readonly Color Ayuda = Color.FromArgb("#b8c4b0");
    private static readonly Color Placeholder = Color.FromArgb("#8a9a90");
    private static readonly Color Etiqueta = Color.FromArgb("#d4c48a");

    private readonly NewRichApiClient _api;
    private readonly ITokenStore _tokens;
    private readonly SesionPda _sesion;
    private readonly NavegadorApp _nav;
    private readonly SincronizacionOfflineServicio _offline;
    private readonly CodigosOfflineEnVivoServicio _enVivo;
    private readonly Entry _actual;
    private readonly Entry _nueva;
    private readonly Entry _confirma;
    private readonly Label _error = new()
    {
        TextColor = Color.FromArgb("#e8a0a0"),
        FontSize = 12,
        Style = null
    };

    public PasswordPage(
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

        _actual = CampoTexto(PdaTexts.PlaceholderContrasenaActual);
        _nueva = CampoTexto(PdaTexts.PlaceholderNuevaContrasena);
        _confirma = CampoTexto(PdaTexts.PlaceholderConfirmarContrasena);

        var volver = new ImageButton
        {
            Style = null,
            Source = "login_icon_arrow.png",
            Rotation = 180,
            BackgroundColor = Colors.Transparent,
            WidthRequest = 36,
            HeightRequest = 36,
            HorizontalOptions = LayoutOptions.Start,
            Padding = 4
        };
        SemanticProperties.SetDescription(volver, PdaTexts.Volver);
        volver.Clicked += async (_, _) => await VolverAlLoginAsync();

        var guardar = BotonGuardar();

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
                Spacing = 10,
                Children =
                {
                    new Label
                    {
                        Style = null,
                        Text = PdaTexts.PrimerAcceso,
                        FontSize = 11,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = TrazoOro,
                        CharacterSpacing = 2.4
                    },
                    new Label
                    {
                        Style = null,
                        Text = PdaTexts.EstablecerNuevaContrasena,
                        FontSize = 24,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Titulo,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    new Label
                    {
                        Style = null,
                        Text = PdaTexts.PrimerAccesoAyuda,
                        FontSize = 13,
                        TextColor = Ayuda,
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    EtiquetaCampo(PdaTexts.ContrasenaActual),
                    CampoConIcono(_actual, Ojo(_actual)),
                    EtiquetaCampo(PdaTexts.NuevaContrasena),
                    CampoConIcono(_nueva, Ojo(_nueva)),
                    EtiquetaCampo(PdaTexts.ConfirmarContrasena),
                    CampoConIcono(_confirma, Ojo(_confirma)),
                    guardar,
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
                        Padding = new Thickness(22, 16, 22, 16),
                        Spacing = 12,
                        Children =
                        {
                            volver,
                            new Image
                            {
                                Source = "login_marca.png",
                                Aspect = Aspect.AspectFit,
                                HeightRequest = 72,
                                HorizontalOptions = LayoutOptions.Center
                            },
                            tarjeta,
                            new Label
                            {
                                Style = null,
                                Text = PdaTexts.AccesoPie,
                                FontSize = 10,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = TrazoOro,
                                HorizontalTextAlignment = TextAlignment.Center,
                                CharacterSpacing = 0.8,
                                Margin = new Thickness(0, 8, 0, 12)
                            }
                        }
                    }
                }
            }
        };
    }

    private async Task VolverAlLoginAsync()
    {
        await _tokens.BorrarAsync();
        _nav.IrALogin();
    }

    private View BotonGuardar()
    {
        var boton = new Button
        {
            Style = null,
            Text = PdaTexts.GuardarYContinuar,
            BackgroundColor = BotonOro,
            TextColor = TintaOro,
            FontAttributes = FontAttributes.Bold,
            FontSize = 16,
            CornerRadius = 22,
            HeightRequest = 48,
            Padding = new Thickness(12, 0, 48, 0)
        };
        boton.Clicked += async (_, _) => await GuardarAsync();

        return new Grid
        {
            HeightRequest = 48,
            Children =
            {
                boton,
                new Image
                {
                    Source = "login_icon_arrow.png",
                    WidthRequest = 26,
                    HeightRequest = 26,
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 0, 12, 0),
                    InputTransparent = true
                }
            }
        };
    }

    private static Label EtiquetaCampo(string texto) => new()
    {
        Style = null,
        Text = texto,
        FontSize = 12,
        FontAttributes = FontAttributes.Bold,
        TextColor = Etiqueta
    };

    private static Entry CampoTexto(string placeholder) => new()
    {
        Style = null,
        Placeholder = placeholder,
        PlaceholderColor = Placeholder,
        IsPassword = true,
        BackgroundColor = Colors.Transparent,
        TextColor = Titulo,
        FontSize = 15,
        VerticalOptions = LayoutOptions.Center,
        ReturnType = ReturnType.Done
    };

    private static ImageButton Ojo(Entry entrada)
    {
        var ver = new ImageButton
        {
            Style = null,
            Source = "login_icon_eye.png",
            BackgroundColor = Colors.Transparent,
            WidthRequest = 36,
            HeightRequest = 36,
            Padding = 4,
            VerticalOptions = LayoutOptions.Center
        };
        SemanticProperties.SetDescription(ver, PdaTexts.MostrarContrasena);
        ver.Clicked += (_, _) =>
        {
            entrada.IsPassword = !entrada.IsPassword;
            SemanticProperties.SetDescription(
                ver,
                entrada.IsPassword ? PdaTexts.MostrarContrasena : PdaTexts.OcultarContrasena);
        };
        return ver;
    }

    private static Border CampoConIcono(Entry entrada, View extra)
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
        fila.Add(new Image
        {
            Source = "login_icon_lock.png",
            WidthRequest = 28,
            HeightRequest = 28,
            VerticalOptions = LayoutOptions.Center
        }, 0);
        fila.Add(entrada, 1);
        fila.Add(extra, 2);

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

    private async Task GuardarAsync()
    {
        _error.Text = string.Empty;
        var resultado = await _api.CambiarPasswordAsync(new CambiarPasswordRequest
        {
            PasswordActual = _actual.Text ?? string.Empty,
            PasswordNuevo = _nueva.Text ?? string.Empty,
            PasswordConfirmacion = _confirma.Text ?? string.Empty
        }, CancellationToken.None);

        if (!resultado.IsSuccess || resultado.Data is null)
        {
            _error.Text = resultado.Message;
            return;
        }

        await _tokens.GuardarAsync(resultado.Data.Token);
        _sesion.Usuario = resultado.Data;
        var shell = NavegacionPorRol.Para(resultado.Data.Rol);
        if (!shell.IsSuccess)
        {
            _error.Text = shell.Message;
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
}
