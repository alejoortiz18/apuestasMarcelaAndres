using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Auth;
using NewRich.Pda.Core.Ventas;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Vendedor;

public sealed class TipoApuestaPage : ContentPage
{
    private readonly SesionPda _sesion;
    private readonly IServiceProvider _services;

    public TipoApuestaPage(SesionPda sesion, IServiceProvider services)
    {
        _sesion = sesion;
        _services = services;
        Title = PdaTexts.JuegoNuevo;
        BackgroundColor = Ui.Paper;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _sesion.HorarioCerrado = HorarioPda.EstaFuera(
            _sesion.Limites,
            ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        if (!HorarioPda.PuedeIniciarJuegoNuevo(_sesion.HorarioCerrado))
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Children = { Ui.Banner($"{PdaTexts.JuegosCerrados} {PdaTexts.JuegosCerradosVenta}", Ui.DangerBg, Ui.Danger) }
            };
            BarraMenuVendedor.Asegurar(this, "vender");
            return;
        }

        var individual = Individual();
        var juegoNumeros = JuegoNumeros();
        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 12,
            Children =
            {
                new Label { Text = PdaTexts.TipoApuestaAyuda, TextColor = Ui.Muted, FontSize = 13 },
                individual,
                juegoNumeros
            }
        };
        BarraMenuVendedor.Asegurar(this, "vender");
    }

    private Button Individual()
    {
        var boton = new Button
        {
            Text = $"{PdaTexts.TipoIndividual}\n{PdaTexts.TipoIndividualAyuda} Máximo {_sesion.Limites.MaxLineasIndividual} líneas.",
            BackgroundColor = Colors.White,
            TextColor = Ui.Ink,
            BorderColor = Ui.Line,
            BorderWidth = 2,
            CornerRadius = 14,
            HeightRequest = 90
        };
        boton.Clicked += async (_, _) =>
        {
            _sesion.Borrador = TicketDraft.Crear(TipoApuesta.INDIVIDUAL, _sesion.Limites.MaxLineasIndividual);
            await Navigation.PushAsync(_services.GetRequiredService<ConstruirApuestaPage>());
        };
        return boton;
    }

    private Button JuegoNumeros()
    {
        var boton = new Button
        {
            Text = $"{JuegoNumerosManualTexts.BotonTipo}\n{JuegoNumerosManualTexts.BotonTipoAyuda}",
            BackgroundColor = Colors.White,
            TextColor = Ui.Ink,
            BorderColor = Ui.Line,
            BorderWidth = 2,
            CornerRadius = 14,
            HeightRequest = 90
        };
        boton.Clicked += async (_, _) =>
            await Navigation.PushAsync(_services.GetRequiredService<JuegoNumerosManualPage>());
        return boton;
    }
}
