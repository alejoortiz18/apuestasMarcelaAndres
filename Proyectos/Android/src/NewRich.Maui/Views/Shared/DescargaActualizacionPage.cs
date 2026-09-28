using NewRich.Pda.Core;

namespace NewRich.Maui.Views.Shared;

public sealed class DescargaActualizacionPage : ContentPage
{
    private readonly BoxView _relleno = new()
    {
        Color = Color.FromArgb("#C9A44A"),
        HeightRequest = 7,
        HorizontalOptions = LayoutOptions.Start,
        VerticalOptions = LayoutOptions.Fill,
        CornerRadius = 4
    };
    private readonly Label _paso = new()
    {
        Text = DescargaActualizacion.Preparando().Etiqueta,
        FontSize = 13,
        TextColor = Color.FromArgb("#E8D5A3"),
        HorizontalTextAlignment = TextAlignment.Center
    };
    private readonly Action? _alRegresar;
    private double _anchoPista;
    private double _fraccion;
    private bool _yaAparecio;
    private bool _salio;
    private bool _vigilarRegreso;

    public DescargaActualizacionPage(Action? alRegresar = null)
    {
        _alRegresar = alRegresar;
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
            ActualizarBarra(_fraccion);
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
                new VerticalStackLayout
                {
                    Spacing = 10,
                    Padding = new Thickness(36, 0, 36, 36),
                    VerticalOptions = LayoutOptions.End,
                    Children = { _paso, pista }
                }
            }
        };
    }

    public void VigilarRegreso() => _vigilarRegreso = true;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (!_yaAparecio)
        {
            _yaAparecio = true;
            return;
        }

        if (!_vigilarRegreso || !_salio)
        {
            return;
        }

        _vigilarRegreso = false;
        _salio = false;
        _alRegresar?.Invoke();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_vigilarRegreso)
        {
            _salio = true;
        }
    }

    protected override bool OnBackButtonPressed() => true;

    public void Aplicar(AvanceDescarga avance)
    {
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(() => Aplicar(avance));
            return;
        }

        _paso.Text = avance.Etiqueta;
        ActualizarBarra(avance.Fraccion);
        SemanticScreenReader.Announce($"{avance.Etiqueta}. {(int)Math.Round(avance.Fraccion * 100)} por ciento.");
    }

    private void ActualizarBarra(double fraccion)
    {
        _fraccion = Math.Clamp(fraccion, 0, 1);
        if (_anchoPista <= 0)
        {
            return;
        }

        var ancho = Math.Clamp(fraccion, 0, 1) * _anchoPista;
        _relleno.WidthRequest = fraccion <= 0 ? 0 : Math.Max(8, ancho);
    }
}
