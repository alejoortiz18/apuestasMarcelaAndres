using NewRich.Application.Contracts.Usuarios;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;

namespace NewRich.Maui.Views.Observador;

public sealed class VendedoresAccionesPage : ContentPage
{
    private static readonly (string Nombre, EstadoFiltroVendedor Valor)[] OpcionesEstado =
    [
        (PdaTexts.EstadoTodos, EstadoFiltroVendedor.Todos),
        (PdaTexts.EstadoBloqueados, EstadoFiltroVendedor.Bloqueados),
        (PdaTexts.EstadoSinBloqueo, EstadoFiltroVendedor.SinBloqueo)
    ];

    private readonly NewRichApiClient _api;
    private readonly VerticalStackLayout _lista = new() { Spacing = 10 };
    private readonly Label _aviso = new() { FontSize = 13, TextColor = Ui.Muted, LineBreakMode = LineBreakMode.WordWrap };
    private readonly Label _resumen = new() { FontSize = 12, TextColor = Ui.Muted };
    private readonly Entry _buscar = Ui.Entrada(PdaTexts.BuscarVendedorAyuda);
    private readonly Picker _grupo = new() { Title = PdaTexts.Grupo, TextColor = Ui.Ink, BackgroundColor = Color.FromArgb("#fbfdfb") };
    private readonly Picker _estado = new() { Title = PdaTexts.EstadoVendedor, TextColor = Ui.Ink, BackgroundColor = Color.FromArgb("#fbfdfb") };
    private IReadOnlyList<UsuarioResponse> _usuarios = [];
    private IReadOnlyList<OpcionGrupoVendedor> _grupos = [];

    public VendedoresAccionesPage(NewRichApiClient api)
    {
        _api = api;
        Title = PdaTexts.VendedoresAcciones;
        BackgroundColor = Ui.Paper;

        _buscar.ReturnType = ReturnType.Search;
        _buscar.ClearButtonVisibility = ClearButtonVisibility.WhileEditing;
        SemanticProperties.SetDescription(_buscar, PdaTexts.BuscarVendedor);
        SemanticProperties.SetDescription(_grupo, PdaTexts.Grupo);
        SemanticProperties.SetDescription(_estado, PdaTexts.EstadoVendedor);
        _estado.ItemsSource = OpcionesEstado.Select(o => o.Nombre).ToList();
        _estado.SelectedIndex = 0;

        _buscar.TextChanged += (_, _) => Pintar();
        _grupo.SelectedIndexChanged += (_, _) => Pintar();
        _estado.SelectedIndexChanged += (_, _) => Pintar();

        var limpiar = Ui.Secundario(PdaTexts.LimpiarFiltros);
        limpiar.Clicked += (_, _) => LimpiarFiltros();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 12,
                Children =
                {
                    new Label { Text = PdaTexts.VendedoresAccionesSub, FontSize = 13, TextColor = Ui.Muted },
                    Ui.Tarjeta(new VerticalStackLayout
                    {
                        Spacing = 8,
                        Children =
                        {
                            Ui.Campo(PdaTexts.BuscarVendedor),
                            _buscar,
                            new Grid
                            {
                                ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
                                ColumnSpacing = 10,
                                Children =
                                {
                                    Columna(0, PdaTexts.Grupo, _grupo),
                                    Columna(1, PdaTexts.EstadoVendedor, _estado)
                                }
                            },
                            _resumen,
                            limpiar
                        }
                    }),
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

    private static View Columna(int columna, string etiqueta, Picker picker)
    {
        var contenido = new VerticalStackLayout { Spacing = 4, Children = { Ui.Campo(etiqueta), picker } };
        Grid.SetColumn(contenido, columna);
        return contenido;
    }

    private async Task CargarAsync()
    {
        _aviso.Text = string.Empty;
        _aviso.TextColor = Ui.Muted;
        var resultado = await _api.UsuariosAsync(CancellationToken.None);
        if (!resultado.IsSuccess)
        {
            _lista.Children.Clear();
            _resumen.Text = string.Empty;
            _aviso.Text = resultado.Message;
            _aviso.TextColor = Ui.Danger;
            return;
        }

        _usuarios = resultado.Data ?? [];
        var grupoAnterior = GrupoSeleccionado();
        _grupos = FiltroVendedores.Grupos(_usuarios);
        _grupo.ItemsSource = _grupos.Select(g => g.Nombre).ToList();
        var indice = grupoAnterior is null
            ? 0
            : _grupos.ToList().FindIndex(g => g.GrupoId == grupoAnterior.GrupoId && g.SinGrupo == grupoAnterior.SinGrupo);
        _grupo.SelectedIndex = Math.Max(indice, 0);
        Pintar();
    }

    private OpcionGrupoVendedor? GrupoSeleccionado() =>
        _grupo.SelectedIndex >= 0 && _grupo.SelectedIndex < _grupos.Count ? _grupos[_grupo.SelectedIndex] : null;

    private EstadoFiltroVendedor EstadoSeleccionado() =>
        _estado.SelectedIndex >= 0 && _estado.SelectedIndex < OpcionesEstado.Length
            ? OpcionesEstado[_estado.SelectedIndex].Valor
            : EstadoFiltroVendedor.Todos;

    private void LimpiarFiltros()
    {
        _buscar.Text = string.Empty;
        _grupo.SelectedIndex = _grupos.Count > 0 ? 0 : -1;
        _estado.SelectedIndex = 0;
        Pintar();
    }

    private void Pintar()
    {
        _lista.Children.Clear();
        var total = FiltroVendedores.Aplicar(_usuarios, null, null, EstadoFiltroVendedor.Todos).Count;
        var vendedores = FiltroVendedores.Aplicar(_usuarios, _buscar.Text, GrupoSeleccionado(), EstadoSeleccionado());
        _resumen.Text = total == 0 ? string.Empty : FiltroVendedores.Resumen(vendedores.Count, total);
        _aviso.TextColor = Ui.Muted;
        if (total == 0)
        {
            _aviso.Text = PdaTexts.SinVendedores;
            return;
        }

        _aviso.Text = vendedores.Count == 0 ? PdaTexts.SinVendedoresConFiltros : string.Empty;
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
            await DisplayAlert(PdaTexts.VendedoresAcciones, r.Message, PdaTexts.Aceptar);
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
        var detalle = $"{vendedor.Usuario} · {PdaTexts.Documento}: {(string.IsNullOrWhiteSpace(vendedor.Documento) ? "-" : vendedor.Documento)}";
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
                    new Label { Text = detalle, FontSize = 13, TextColor = Ui.Muted },
                    new Label { Text = $"{PdaTexts.Grupo}: {vendedor.GrupoNombre ?? PdaTexts.SinGrupo}", FontSize = 13, TextColor = Ui.Muted },
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
