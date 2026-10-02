using NewRich.Application.Contracts.Usuarios;
using NewRich.Domain.Enums;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;

namespace NewRich.Maui.Views.Observador;

public sealed class VendedoresAccionesPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly VerticalStackLayout _lista = new() { Spacing = 10 };
    private readonly Label _aviso = new() { FontSize = 13, TextColor = Ui.Muted };

    public VendedoresAccionesPage(NewRichApiClient api)
    {
        _api = api;
        Title = PdaTexts.VendedoresAcciones;
        BackgroundColor = Ui.Paper;
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 12,
                Children =
                {
                    new Label { Text = PdaTexts.VendedoresAccionesSub, FontSize = 13, TextColor = Ui.Muted },
                    _aviso,
                    _lista
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarAsync();
    }

    private async Task CargarAsync()
    {
        _lista.Children.Clear();
        _aviso.Text = string.Empty;
        var resultado = await _api.UsuariosAsync(CancellationToken.None);
        if (!resultado.IsSuccess)
        {
            _aviso.Text = resultado.Message;
            _aviso.TextColor = Ui.Danger;
            return;
        }

        var vendedores = (resultado.Data ?? []).Where(u => u.Rol == RolUsuario.Vendedor).OrderBy(u => u.NombreCompleto).ToList();
        if (vendedores.Count == 0)
        {
            _aviso.Text = PdaTexts.SinVendedores;
            return;
        }

        foreach (var vendedor in vendedores)
        {
            _lista.Children.Add(CrearTarjeta(vendedor));
        }
    }

    private View CrearTarjeta(UsuarioResponse vendedor)
    {
        var desbloquear = new Button
        {
            Text = PdaTexts.DesbloquearVendedor,
            IsEnabled = vendedor.EstadoBloqueado,
            BackgroundColor = Ui.Dark,
            TextColor = Colors.White
        };
        desbloquear.Clicked += async (_, _) =>
        {
            var r = await _api.DesbloquearUsuarioAsync(vendedor.UsuarioId, CancellationToken.None);
            await DisplayAlert(PdaTexts.VendedoresAcciones, r.IsSuccess ? r.Message : r.Message, PdaTexts.Aceptar);
            await CargarAsync();
        };
        var restablecer = new Button
        {
            Text = PdaTexts.RestablecerContrasenaVendedor,
            BackgroundColor = Ui.Green,
            TextColor = Colors.White
        };
        restablecer.Clicked += async (_, _) =>
        {
            var r = await _api.RestablecerPasswordAsync(vendedor.UsuarioId, CancellationToken.None);
            var texto = r.IsSuccess
                ? $"{r.Message}\n{PdaTexts.PasswordTemporalMostrada}: {r.Data!.PasswordTemporal}"
                : r.Message;
            await DisplayAlert(PdaTexts.VendedoresAcciones, texto, PdaTexts.Aceptar);
            await CargarAsync();
        };
        return new Frame
        {
            BorderColor = Ui.Line,
            BackgroundColor = Colors.White,
            Padding = 12,
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = vendedor.NombreCompleto, FontAttributes = FontAttributes.Bold, TextColor = Ui.Ink },
                    new Label { Text = vendedor.Usuario, FontSize = 13, TextColor = Ui.Muted },
                    new Label
                    {
                        Text = vendedor.EstadoBloqueado ? PdaTexts.VendedorBloqueado : PdaTexts.VendedorActivo,
                        FontSize = 13,
                        TextColor = vendedor.EstadoBloqueado ? Ui.Danger : Ui.Green
                    },
                    desbloquear,
                    restablecer
                }
            }
        };
    }
}
