using NewRich.Pda.Core;
using NewRich.Pda.Core.Auth;
using NewRich.Pda.Core.Ventas;
using NewRich.Maui.Data;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Vendedor;

public sealed class NumerosBloqueadosPage : ContentPage
{
    private readonly SesionPda _sesion;
    private readonly LocalDatabase _offline;
    private readonly VerticalStackLayout _lista = new() { Spacing = 8 };

    public NumerosBloqueadosPage(SesionPda sesion, LocalDatabase offline)
    {
        _sesion = sesion;
        _offline = offline;
        Title = PdaTexts.NumerosBloqueados;
        BackgroundColor = Ui.Paper;
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 10,
                Children =
                {
                    new Label { Text = PdaTexts.NumerosBloqueadosAyuda, TextColor = Ui.Muted, FontSize = 12 },
                    _lista
                }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var guardados = await _offline.NumerosRestringidosAsync();
        Pintar(NumerosRestringidosPda.Vigentes(
            guardados.Count > 0 ? guardados : null,
            _sesion.Limites.NumerosRestringidos));
    }

    private void Pintar(IReadOnlyList<string> numeros)
    {
        _lista.Children.Clear();
        if (numeros.Count == 0)
        {
            _lista.Children.Add(new Label { Text = PdaTexts.SinNumerosBloqueados, TextColor = Ui.Muted });
            return;
        }

        foreach (var numero in numeros)
        {
            _lista.Children.Add(Ui.Tarjeta(new Label
            {
                Text = numero,
                FontAttributes = FontAttributes.Bold,
                TextColor = Ui.Ink,
                FontSize = 18
            }));
        }
    }
}
