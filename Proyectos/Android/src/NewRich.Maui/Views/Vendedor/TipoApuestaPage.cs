using NewRich.Domain.Enums;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Auth;
using NewRich.Pda.Core.Ventas;

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
        if (_sesion.HorarioCerrado)
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Children = { Ui.Banner($"{PdaTexts.JuegosCerrados} {PdaTexts.JuegosCerradosVenta}", Ui.DangerBg, Ui.Danger) }
            };
            return;
        }

        var combinada = Tipo(PdaTexts.TipoCombinada, $"{PdaTexts.TipoCombinadaAyuda} Máximo {_sesion.Limites.MaxJuegosCombinado} juego.", TipoApuesta.COMBINADO);
        var individual = Tipo(PdaTexts.TipoIndividual, $"{PdaTexts.TipoIndividualAyuda} Máximo {_sesion.Limites.MaxLineasIndividual} líneas.", TipoApuesta.INDIVIDUAL);
        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 12,
            Children =
            {
                new Label { Text = PdaTexts.TipoApuestaAyuda, TextColor = Ui.Muted, FontSize = 13 },
                combinada,
                individual
            }
        };
    }

    private Button Tipo(string titulo, string ayuda, TipoApuesta tipo)
    {
        var boton = new Button
        {
            Text = $"{titulo}\n{ayuda}",
            BackgroundColor = Colors.White,
            TextColor = Ui.Ink,
            BorderColor = Ui.Line,
            BorderWidth = 2,
            CornerRadius = 14,
            HeightRequest = 90
        };
        boton.Clicked += async (_, _) =>
        {
            var max = tipo == TipoApuesta.COMBINADO ? _sesion.Limites.MaxJuegosCombinado : _sesion.Limites.MaxLineasIndividual;
            _sesion.Borrador = TicketDraft.Crear(tipo, max);
            await Navigation.PushAsync(_services.GetRequiredService<ConstruirApuestaPage>());
        };
        return boton;
    }
}
