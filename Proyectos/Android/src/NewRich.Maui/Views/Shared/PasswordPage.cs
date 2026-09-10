using NewRich.Application.Contracts.Auth;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Services;

namespace NewRich.Maui.Views.Shared;

public sealed class PasswordPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly ITokenStore _tokens;
    private readonly SesionPda _sesion;
    private readonly NavegadorApp _nav;
    private readonly Entry _actual = Ui.Entrada(string.Empty, true);
    private readonly Entry _nueva = Ui.Entrada(string.Empty, true);
    private readonly Entry _confirma = Ui.Entrada(string.Empty, true);
    private readonly Label _error = new() { TextColor = Ui.Danger, FontSize = 12 };

    public PasswordPage(NewRichApiClient api, ITokenStore tokens, SesionPda sesion, NavegadorApp nav)
    {
        _api = api;
        _tokens = tokens;
        _sesion = sesion;
        _nav = nav;
        Title = PdaTexts.EstablecerNuevaContrasena;
        BackgroundColor = Ui.Paper;

        var guardar = Ui.Primario(PdaTexts.GuardarYContinuar);
        guardar.Clicked += async (_, _) => await GuardarAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 28,
                Spacing = 10,
                Children =
                {
                    Ui.Tarjeta(new VerticalStackLayout
                    {
                        Spacing = 10,
                        Children =
                        {
                            Ui.Marca(PdaTexts.PrimerAcceso),
                            new Label { Text = PdaTexts.EstablecerNuevaContrasena, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                            new Label { Text = PdaTexts.PrimerAccesoAyuda, FontSize = 13, TextColor = Ui.Muted },
                            Ui.Campo(PdaTexts.ContrasenaActual),
                            _actual,
                            Ui.Campo(PdaTexts.NuevaContrasena),
                            _nueva,
                            Ui.Campo(PdaTexts.ConfirmarContrasena),
                            _confirma,
                            guardar,
                            _error
                        }
                    })
                }
            }
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
            _nav.IrAVendedor();
        }
        else
        {
            _nav.IrAObservador();
        }
    }
}
