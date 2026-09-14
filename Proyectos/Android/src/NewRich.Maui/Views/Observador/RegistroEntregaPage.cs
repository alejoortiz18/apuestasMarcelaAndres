using NewRich.Application.Contracts.Premios;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;

namespace NewRich.Maui.Views.Observador;

public sealed class RegistroEntregaPage : ContentPage
{
    private readonly NewRichApiClient _api;
    private readonly SesionPda _sesion;
    private readonly CasoGanadorResponse _caso;
    private readonly Entry _nombre = Ui.Entrada(PdaTexts.NombreGanador);
    private readonly Entry _apellido = Ui.Entrada(PdaTexts.ApellidoGanador);
    private readonly Entry _contacto = Ui.Entero(PdaTexts.NumeroContacto, 15);
    private readonly Entry _lugar = Ui.Entrada(PdaTexts.LugarGano);
    private readonly Entry _valor = Ui.Entrada(PdaTexts.ValorTotalGanado);
    private readonly Label _vendedor;
    private readonly Label _entrega;
    private readonly Label _estadoFotoTicket = EstadoFoto();
    private readonly Label _estadoFotoGanador = EstadoFoto();
    private readonly Label _estadoFotoCedula = EstadoFoto();
    private readonly Label _pendiente = new() { FontSize = 12, TextColor = Ui.Warn };
    private readonly Label _aviso = new() { FontSize = 12, TextColor = Ui.Danger };
    private readonly Button _registrar;
    private EvidenciaFotoRequest? _fotoTicket;
    private EvidenciaFotoRequest? _fotoGanador;
    private EvidenciaFotoRequest? _fotoCedula;
    private bool _enviando;

    public RegistroEntregaPage(NewRichApiClient api, SesionPda sesion, CasoGanadorResponse caso)
    {
        _api = api;
        _sesion = sesion;
        _caso = caso;
        Title = PdaTexts.RegistrarGanador;
        BackgroundColor = Ui.Paper;
        _vendedor = SoloLectura(caso.Vendedor);
        _entrega = SoloLectura(sesion.Usuario?.NombreCompleto ?? string.Empty);
        _valor.Keyboard = Keyboard.Numeric;
        _registrar = Ui.Primario(PdaTexts.RegistrarEntregaPremio);
        _registrar.Clicked += async (_, _) => await RegistrarAsync();
        foreach (var entrada in new[] { _nombre, _apellido, _contacto, _lugar, _valor })
        {
            entrada.TextChanged += (_, _) => ActualizarEstadoBoton();
        }

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 16,
                Spacing = 10,
                Children =
                {
                    Ui.Tarjeta(new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children =
                        {
                            new Label
                            {
                                Text = $"Ticket {_caso.Ticket}",
                                FontAttributes = FontAttributes.Bold,
                                FontSize = 16,
                                TextColor = Ui.Ink
                            },
                            new Label
                            {
                                Text = $"Estado: {_caso.Estado}",
                                FontSize = 12,
                                TextColor = Ui.Muted
                            }
                        }
                    }),
                    Ui.Campo(PdaTexts.NombreGanador),
                    _nombre,
                    Ui.Campo(PdaTexts.ApellidoGanador),
                    _apellido,
                    Ui.Campo(PdaTexts.NumeroContacto),
                    _contacto,
                    Ui.Campo(PdaTexts.LugarGano),
                    _lugar,
                    Ui.Campo(PdaTexts.NombreVendedor),
                    _vendedor,
                    Ui.Campo(PdaTexts.ValorTotalGanado),
                    _valor,
                    Ui.Campo(PdaTexts.PersonaQueEntrega),
                    _entrega,
                    BloqueFoto(PdaTexts.FotoTicketConQr, _estadoFotoTicket,
                        () => CapturarAsync(f => _fotoTicket = f, _estadoFotoTicket, true),
                        () => CapturarAsync(f => _fotoTicket = f, _estadoFotoTicket, false)),
                    BloqueFoto(PdaTexts.FotoGanadorConTicket, _estadoFotoGanador,
                        () => CapturarAsync(f => _fotoGanador = f, _estadoFotoGanador, true),
                        () => CapturarAsync(f => _fotoGanador = f, _estadoFotoGanador, false)),
                    BloqueFoto(PdaTexts.FotoCedula, _estadoFotoCedula,
                        () => CapturarAsync(f => _fotoCedula = f, _estadoFotoCedula, true),
                        () => CapturarAsync(f => _fotoCedula = f, _estadoFotoCedula, false)),
                    _pendiente,
                    _aviso,
                    _registrar
                }
            }
        };
        ActualizarEstadoBoton();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _api.IniciarRegistroPremioAsync(_caso.CasoId, CancellationToken.None);
        }
        catch (Exception)
        {
        }
    }

    private async Task CapturarAsync(Action<EvidenciaFotoRequest> asignar, Label estado, bool camara)
    {
        try
        {
            FileResult? archivo;
            if (camara)
            {
                var permiso = await Permissions.RequestAsync<Permissions.Camera>();
                if (permiso != PermissionStatus.Granted)
                {
                    await this.AvisoAsync(PdaTexts.RegistrarGanador, PdaTexts.ObservadorCamaraNoDisponible, PdaTexts.Cerrar);
                    return;
                }

                archivo = await MediaPicker.Default.CapturePhotoAsync();
            }
            else
            {
                archivo = await MediaPicker.Default.PickPhotoAsync();
            }

            if (archivo is null)
            {
                return;
            }

            await using var stream = await archivo.OpenReadAsync();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            asignar(new EvidenciaFotoRequest
            {
                NombreArchivo = string.IsNullOrWhiteSpace(archivo.FileName) ? "evidencia.jpg" : archivo.FileName,
                ContenidoBase64 = Convert.ToBase64String(buffer.ToArray())
            });
            estado.Text = PdaTexts.FotoCargada;
            estado.TextColor = Ui.Green;
            ActualizarEstadoBoton();
        }
        catch (Exception)
        {
            await this.AvisoAsync(PdaTexts.RegistrarGanador, PdaTexts.ObservadorCamaraNoDisponible, PdaTexts.Cerrar);
        }
    }

    private async Task RegistrarAsync()
    {
        if (_enviando || !_registrar.IsEnabled)
        {
            return;
        }

        _enviando = true;
        _aviso.Text = string.Empty;
        try
        {
            if (!decimal.TryParse(_valor.Text?.Trim(), out var monto) || monto <= 0)
            {
                _aviso.Text = $"{PdaTexts.FaltaCompletar} {PdaTexts.ValorTotalGanado}";
                return;
            }

            var resultado = await _api.RegistrarEntregaPremioAsync(_caso.CasoId, new RegistrarEntregaPremioRequest
            {
                NombreGanador = _nombre.Text?.Trim() ?? string.Empty,
                ApellidoGanador = _apellido.Text?.Trim() ?? string.Empty,
                NumeroContacto = _contacto.Text?.Trim() ?? string.Empty,
                LugarGano = _lugar.Text?.Trim() ?? string.Empty,
                ValorTotalGanado = monto,
                FotoTicketConQr = _fotoTicket,
                FotoGanadorConTicket = _fotoGanador,
                FotoCedula = _fotoCedula
            }, CancellationToken.None);

            if (!resultado.IsSuccess)
            {
                _aviso.Text = resultado.Message;
                _aviso.TextColor = Ui.Danger;
                return;
            }

            await MostrarComprobanteAsync(resultado.Data, monto);
        }
        catch (Exception)
        {
            _aviso.Text = PdaTexts.SinConexionServidor;
            _aviso.TextColor = Ui.Danger;
        }
        finally
        {
            _enviando = false;
            ActualizarEstadoBoton();
        }
    }

    private async Task MostrarComprobanteAsync(CasoGanadorResponse? registrado, decimal monto)
    {
        var fecha = registrado?.FechaRegistro ?? DateTime.UtcNow;
        var datos = new ComprobanteEntregaDatos
        {
            Ticket = registrado?.Ticket ?? _caso.Ticket,
            NombreGanador = _nombre.Text?.Trim() ?? string.Empty,
            ApellidoGanador = _apellido.Text?.Trim() ?? string.Empty,
            NumeroContacto = _contacto.Text?.Trim() ?? string.Empty,
            LugarGano = _lugar.Text?.Trim() ?? string.Empty,
            NombreVendedor = registrado?.Vendedor ?? _caso.Vendedor,
            ValorTotalGanado = monto,
            PersonaQueEntrega = _sesion.Usuario?.NombreCompleto ?? string.Empty,
            FechaEntrega = fecha.Kind == DateTimeKind.Utc ? fecha.ToLocalTime() : fecha
        };

        var evidencias = new List<(string, byte[])>();
        Agregar(evidencias, PdaTexts.FotoTicketConQr, _fotoTicket);
        Agregar(evidencias, PdaTexts.FotoGanadorConTicket, _fotoGanador);
        Agregar(evidencias, PdaTexts.FotoCedula, _fotoCedula);

        var actual = this;
        await Navigation.PushAsync(new ComprobanteEntregaPage(datos, evidencias));
        Navigation.RemovePage(actual);
    }

    private static void Agregar(List<(string, byte[])> destino, string titulo, EvidenciaFotoRequest? foto)
    {
        if (string.IsNullOrWhiteSpace(foto?.ContenidoBase64))
        {
            return;
        }

        destino.Add((titulo, Convert.FromBase64String(foto.ContenidoBase64)));
    }

    private void ActualizarEstadoBoton()
    {
        var completo = EntregaPremioFormulario.EstaCompleto(
            _nombre.Text,
            _apellido.Text,
            _contacto.Text,
            _lugar.Text,
            _valor.Text,
            _fotoTicket is not null,
            _fotoGanador is not null,
            _fotoCedula is not null);
        _registrar.IsEnabled = completo && !_enviando;
        _registrar.Opacity = completo ? 1 : 0.45;
        var pendiente = EntregaPremioFormulario.Pendiente(
            _nombre.Text,
            _apellido.Text,
            _contacto.Text,
            _lugar.Text,
            _valor.Text,
            _fotoTicket is not null,
            _fotoGanador is not null,
            _fotoCedula is not null);
        _pendiente.Text = pendiente is null ? string.Empty : $"{PdaTexts.FaltaCompletar} {pendiente}";
    }

    private static View BloqueFoto(string titulo, Label estado, Func<Task> tomar, Func<Task> galeria)
    {
        var camara = Ui.Secundario(PdaTexts.TomarFoto);
        camara.Clicked += async (_, _) => await tomar();
        var elegir = Ui.Secundario(PdaTexts.ElegirGaleria);
        elegir.Clicked += async (_, _) => await galeria();
        return new VerticalStackLayout
        {
            Spacing = 6,
            Children =
            {
                Ui.Campo(titulo),
                estado,
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection { new(GridLength.Star), new(GridLength.Star) },
                    ColumnSpacing = 8,
                    Children =
                    {
                        Colocar(camara, 0),
                        Colocar(elegir, 1)
                    }
                }
            }
        };
    }

    private static View Colocar(View vista, int columna)
    {
        Grid.SetColumn(vista, columna);
        return vista;
    }

    private static Label SoloLectura(string texto) => new()
    {
        Text = texto,
        FontSize = 15,
        FontAttributes = FontAttributes.Bold,
        TextColor = Ui.Ink,
        BackgroundColor = Colors.White,
        Padding = new Thickness(12, 10)
    };

    private static Label EstadoFoto() => new()
    {
        Text = "Pendiente",
        FontSize = 12,
        TextColor = Ui.Muted
    };
}
