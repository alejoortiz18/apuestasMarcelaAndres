using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Maui.Data;
using NewRich.Maui.Services;
using NewRich.Maui.Views;

namespace NewRich.Maui.Views.Shared;

public sealed class ArranquePage : ContentPage
{
    private readonly LocalDatabase _db;
    private readonly ITokenStore _tokens;
    private readonly NewRichApiClient _api;
    private readonly NavegadorApp _nav;
    private readonly BoxView _relleno = new()
    {
        Color = Ui.Gold,
        HeightRequest = 7,
        HorizontalOptions = LayoutOptions.Start,
        VerticalOptions = LayoutOptions.Fill,
        CornerRadius = 4
    };
    private readonly Label _paso = new()
    {
        Text = ArranqueSecuencia.Etiqueta(0),
        FontSize = 12,
        TextColor = Color.FromArgb("#E8D5A3"),
        HorizontalTextAlignment = TextAlignment.Center
    };
    private readonly Label _eslogan = new()
    {
        Text = PdaTexts.ArranqueEslogan,
        FontSize = 11,
        FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb("#C4A35A"),
        HorizontalTextAlignment = TextAlignment.Center,
        CharacterSpacing = 0.6
    };
    private double _anchoPista;
    private bool _inicio;

    public ArranquePage(LocalDatabase db, ITokenStore tokens, NewRichApiClient api, NavegadorApp nav)
    {
        _db = db;
        _tokens = tokens;
        _api = api;
        _nav = nav;
        NavigationPage.SetHasNavigationBar(this, false);
        BackgroundColor = Colors.Black;

        var pista = new Grid
        {
            HeightRequest = 7,
            HorizontalOptions = LayoutOptions.Fill,
            Children =
            {
                new BoxView
                {
                    Color = Color.FromArgb("#3A2E1A"),
                    CornerRadius = 4
                },
                _relleno
            }
        };
        pista.SizeChanged += (_, _) =>
        {
            _anchoPista = pista.Width;
            ActualizarBarra(ArranqueSecuencia.Fraccion(0));
        };

        var pie = new VerticalStackLayout
        {
            Spacing = 10,
            Padding = new Thickness(36, 0, 36, 36),
            VerticalOptions = LayoutOptions.End,
            Children = { _paso, pista, _eslogan }
        };

        Content = new Grid
        {
            Children =
            {
                new Image
                {
                    Source = "presentacion.jpg",
                    Aspect = Aspect.AspectFill,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill
                },
                new BoxView
                {
                    Color = Color.FromArgb("#B3000000"),
                    HeightRequest = 168,
                    VerticalOptions = LayoutOptions.End
                },
                pie
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_inicio)
        {
            return;
        }

        _inicio = true;
        await CargarAsync();
    }

    private async Task CargarAsync()
    {
        try
        {
            await MostrarAsync(0);
            _ = FileSystem.AppDataDirectory;
            await MostrarAsync(1);

            await _db.AsegurarAsync();
            await MostrarAsync(2);

            await _tokens.ObtenerAsync();
            await MostrarAsync(3);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            try
            {
                await _api.ConectarAsync(
                    PdaConexion.UrlsPara(DeviceInfo.Current.DeviceType == DeviceType.Virtual),
                    cts.Token);
            }
            catch (OperationCanceledException)
            {
            }

            await MostrarAsync(4);
            await MostrarAsync(ArranqueSecuencia.Pasos.Count);
        }
        finally
        {
            await Task.Delay(280);
            _nav.IrALogin();
        }
    }

    private async Task MostrarAsync(int pasosCompletados)
    {
        var indice = Math.Min(pasosCompletados, ArranqueSecuencia.Pasos.Count - 1);
        var fraccion = ArranqueSecuencia.Fraccion(pasosCompletados);
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            _paso.Text = ArranqueSecuencia.Etiqueta(indice);
            ActualizarBarra(fraccion);
            SemanticScreenReader.Announce($"{_paso.Text}. {(int)(fraccion * 100)} por ciento.");
        });
        await Task.Delay(120);
    }

    private void ActualizarBarra(double fraccion)
    {
        if (_anchoPista <= 0)
        {
            return;
        }

        _relleno.WidthRequest = Math.Max(8, _anchoPista * fraccion);
    }
}
