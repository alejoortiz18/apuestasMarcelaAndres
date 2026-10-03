using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Services;
using NewRich.Maui.Views.Vendedor;

namespace NewRich.Maui.Views.Observador;

public sealed class ObservadorMasPage : ContentPage
{
    public ObservadorMasPage(
        NavegadorApp nav,
        NewRichApiClient api,
        ITokenStore tokens,
        SesionPda sesion,
        ChatEnVivoServicio chat,
        IServiceProvider services)
    {
        Title = PdaTexts.MasOpciones;
        BackgroundColor = Ui.Paper;

        Button Item(string titulo, string ayuda, Func<Task> accion)
        {
            var b = new Button
            {
                Text = $"{titulo}\n{ayuda}",
                BackgroundColor = Colors.White,
                TextColor = Ui.Ink,
                BorderColor = Ui.Line,
                BorderWidth = 1,
                CornerRadius = 12,
                HeightRequest = 64
            };
            b.Clicked += async (_, _) => await accion();
            return b;
        }

        Label PieVersion()
        {
            var pie = VersionInstaladaPie.Crear();
            Grid.SetRow(pie, 1);
            return pie;
        }

        var menu = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 10,
            Children =
            {
                Item(PdaTexts.ResultadosTitulo, PdaTexts.ResultadosSoloLectura, () => Navigation.PushAsync(services.GetRequiredService<ResultadosPage>())),
                Item(PdaTexts.ValidarTicket, PdaTexts.ValidarTicketAyudaCorta, () => Shell.Current.GoToAsync("//ovalidar")),
                Item(PdaTexts.Kpi, PdaTexts.KpiAyuda, () => Navigation.PushAsync(services.GetRequiredService<KpiPage>())),
                Item(PdaTexts.VendedoresAcciones, PdaTexts.VendedoresAccionesAyuda, () => Navigation.PushAsync(services.GetRequiredService<VendedoresAccionesPage>())),
                Item(PdaTexts.CerrarSesion, PdaTexts.CerrarSesionAyuda, async () =>
                {
                    await chat.DesconectarAsync();
                    await api.LogoutAsync(CancellationToken.None);
                    await tokens.BorrarAsync();
                    sesion.Usuario = null;
                    sesion.Borrador = null;
                    nav.IrALogin();
                })
            }
        };
        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            },
            Children =
            {
                menu,
                PieVersion()
            }
        };
    }
}
