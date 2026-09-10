using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;
using NewRich.Maui.Services;

namespace NewRich.Maui.Views.Vendedor;

public sealed class MasPage : ContentPage
{
    public MasPage(NavegadorApp nav, NewRichApiClient api, ITokenStore tokens, SesionPda sesion, IServiceProvider services)
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

        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 10,
            Children =
            {
                Item(PdaTexts.ResultadosTitulo, PdaTexts.ResultadosSoloLectura, () => Navigation.PushAsync(services.GetRequiredService<ResultadosPage>())),
                Item(PdaTexts.ValidarTicket, PdaTexts.ValidarTicketAyudaCorta, () => Navigation.PushAsync(services.GetRequiredService<ValidarTicketPage>())),
                Item(PdaTexts.ConfiguracionSync, PdaTexts.ConfiguracionSyncAyuda, () => Navigation.PushAsync(services.GetRequiredService<ConfiguracionPage>())),
                Item(PdaTexts.CerrarSesion, PdaTexts.CerrarSesionAyuda, async () =>
                {
                    await api.LogoutAsync(CancellationToken.None);
                    await tokens.BorrarAsync();
                    sesion.Usuario = null;
                    sesion.Borrador = null;
                    nav.IrALogin();
                })
            }
        };
    }
}
