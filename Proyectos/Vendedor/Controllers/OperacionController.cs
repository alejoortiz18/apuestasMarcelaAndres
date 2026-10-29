using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Contracts.Premios;
using NewRich.Application.Contracts.PremiosEntregadosWeb;
using NewRich.Application.Contracts.RegistrarJuegoManual;
using NewRich.Application.Contracts.RegistrarJuegoNumerosWebManual;
using NewRich.Application.Contracts.ResumenVendedorWeb;
using NewRich.Application.Contracts.Ventas;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared;
using NewRich.Vendedor.Constants;
using NewRich.Vendedor.Models;
using NewRich.Vendedor.Services;

namespace NewRich.Vendedor.Controllers;

public sealed class InicioController : VendedorController
{
    private readonly IVendedorApiClient _api;

    public InicioController(IVendedorApiClient api) => _api = api;

    public async Task<IActionResult> Index(int page = 1, int pageSize = 5, CancellationToken cancellationToken = default)
    {
        Nav("inicio", UiTexts.Inicio);
        var resumen = await _api.ResumenHoyAsync(cancellationToken);
        var negado = SiNoAutorizado(resumen);
        if (negado is not null)
        {
            return negado;
        }

        var operativa = await _api.OperativaAsync(cancellationToken);
        var datos = resumen.Data ?? new ResumenVendedorWebResponse();
        var tamano = Paginas.Tamano(pageSize);
        var numero = Paginas.Numero(page, datos.Ganadores.Count, tamano);
        return View(new InicioViewModel
        {
            HorarioCerrado = HorarioCerrado(operativa.Data),
            Aviso = resumen.Success ? null : resumen.Message,
            Resumen = datos,
            Page = numero,
            PageSize = tamano,
            Ganadores = datos.Ganadores.Skip((numero - 1) * tamano).Take(tamano).ToList()
        });
    }
}

public sealed class VenderController : VendedorController
{
    private const decimal ValorMaximoJuego = 999_000m;
    private const int DigitosMaximosNumero = 4;
    private readonly IVendedorApiClient _api;

    public VenderController(IVendedorApiClient api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        Nav("vender", UiTexts.Vender);
        return View(await ArmarAsync(null, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(RegistrarJuegoViewModel model, CancellationToken cancellationToken)
    {
        Nav("vender", UiTexts.Vender);
        var operativo = await _api.OperativaAsync(cancellationToken);
        var negado = SiNoAutorizado(operativo);
        if (negado is not null)
        {
            return negado;
        }

        var filas = model.Filas ?? [];
        var numeros = model.Numeros ?? [];
        var loteriasMarcadas = (model.LoteriasNumeros ?? []).Where(id => id != Guid.Empty).Distinct().ToList();
        var valorEscrito = Pesos(model.ValorJugado, out var valorJugado);
        var horarioCerrado = HorarioCerrado(operativo.Data);
        var usarFilas = model.UsarFilas;
        var usarNumeros = model.UsarNumeros;
        var excedeFilas = usarFilas && filas.Any(f => Pesos(f.Valor, out var v) && v > ValorMaximoJuego);
        var excedeNumeros = usarNumeros && valorEscrito && valorJugado > ValorMaximoJuego;
        if (excedeFilas || excedeNumeros)
        {
            ModelState.AddModelError(string.Empty, UiTexts.ValorMaximoJuego);
            return View(await ArmarAsync(model, cancellationToken));
        }

        if (usarFilas && filas.Any(f => (f.Numero?.Trim().Length ?? 0) > DigitosMaximosNumero))
        {
            ModelState.AddModelError(string.Empty, UiTexts.NumeroMaximoDigitos);
            return View(await ArmarAsync(model, cancellationToken));
        }

        var decision = EvaluacionRegistrarJuego.Evaluar(
            usarFilas
                ? filas.Select(f => new FilaRegistroEntrada(f.LoteriaId != Guid.Empty, Pesos(f.Valor, out _), !string.IsNullOrWhiteSpace(f.Numero))).ToList()
                : [new FilaRegistroEntrada(false, false, false)],
            usarNumeros ? loteriasMarcadas.Count : 0,
            usarNumeros && valorEscrito,
            usarNumeros ? valorJugado : 0,
            usarNumeros ? numeros : [string.Empty],
            operativo.Data?.NumerosRestringidos,
            horarioCerrado,
            usarFilas,
            usarNumeros);

        if (!decision.PuedeRegistrar)
        {
            ModelState.AddModelError(string.Empty, MensajeDecision(decision, horarioCerrado, usarFilas, usarNumeros));
            return View(await ArmarAsync(model, cancellationToken));
        }

        ResultadoFilasJuego? filasResultado = null;
        ResultadoNumerosJuego? numerosResultado = null;
        var conservarFilas = decision.Filas == ParteJuego.Lista;
        var conservarNumeros = decision.Numeros == ParteJuego.Lista;

        if (decision.Filas == ParteJuego.Lista)
        {
            var requestFilas = new List<FilaRegistrarJuegoManualRequest>();
            var filasValidas = true;
            foreach (var fila in filas)
            {
                var numero = fila.Numero?.Trim() ?? string.Empty;
                if (fila.LoteriaId == Guid.Empty || !Pesos(fila.Valor, out var valor) || numero.Length == 0 || numero.Any(c => !char.IsDigit(c)))
                {
                    ModelState.AddModelError(string.Empty, numero.Any(c => !char.IsDigit(c))
                        ? RegistrarJuegoManualMessages.NumeroSoloDigitos
                        : RegistrarJuegoManualMessages.CamposObligatorios);
                    filasValidas = false;
                    break;
                }

                requestFilas.Add(new FilaRegistrarJuegoManualRequest
                {
                    LoteriaId = fila.LoteriaId,
                    Valor = valor,
                    Numero = numero
                });
            }

            if (filasValidas)
            {
            var result = await _api.RegistrarJuegoAsync(new RegistrarJuegoManualRequest
            {
                Filas = requestFilas
            }, string.IsNullOrWhiteSpace(model.Clave) ? Guid.NewGuid().ToString("N") : model.Clave, cancellationToken);
            negado = SiNoAutorizado(result);
            if (negado is not null)
            {
                return negado;
            }

            if (!result.Success || result.Data is null)
            {
                ModelState.AddModelError(string.Empty, result.Message);
            }
            else
            {
                conservarFilas = false;
                filasResultado = new ResultadoFilasJuego
                {
                    Codigo = result.Data.CodigoPublico,
                    Fecha = result.Data.FechaRegistro.ToString("dd-MM-yyyy HH:mm:ss"),
                    Filas = result.Data.Filas.Select(f => new FilaResultadoJuego
                    {
                        Loteria = f.Loteria,
                        Numero = f.Numero,
                        Valor = Montos.De(f.Valor)
                    }).ToList()
                };
            }
            }
        }

        if (decision.Numeros == ParteJuego.Lista)
        {
            var ventaId = Guid.TryParse(model.VentaId, out var id) ? id : Guid.NewGuid();
            var result = await _api.RegistrarJuegoNumerosWebAsync(new RegistrarJuegoNumerosWebManualRequest
            {
                RegistroId = ventaId,
                ValorJugado = valorJugado,
                Numeros = numeros.Select(n => n?.Trim() ?? string.Empty).Where(n => n.Length > 0).ToList(),
                LoteriaIds = loteriasMarcadas
            }, cancellationToken);
            negado = SiNoAutorizado(result);
            if (negado is not null)
            {
                return negado;
            }

            if (!result.Success || result.Data is null)
            {
                ModelState.AddModelError(string.Empty, result.Message);
            }
            else
            {
                conservarNumeros = false;
                numerosResultado = new ResultadoNumerosJuego
                {
                    Codigo = result.Data.CodigoPublico,
                    Fecha = result.Data.FechaRegistro.ToString("dd-MM-yyyy HH:mm:ss"),
                    Filas = result.Data.Apuestas.Select(a => new FilaResultadoJuego
                    {
                        Loteria = a.Loteria,
                        Numero = a.Numero,
                        Valor = Montos.De(a.Valor)
                    }).ToList()
                };
            }
        }

        var vista = await ArmarAsync(model, cancellationToken);
        if (!conservarFilas)
        {
            vista.Filas = [new FilaJuegoForm()];
            vista.Clave = Guid.NewGuid().ToString("N");
            vista.UsarFilas = false;
        }

        if (!conservarNumeros)
        {
            vista.LoteriasNumeros = [];
            vista.ValorJugado = string.Empty;
            vista.Numeros = [string.Empty];
            vista.VentaId = Guid.NewGuid().ToString("N");
            vista.UsarNumeros = false;
        }

        vista.Resultado = filasResultado is null && numerosResultado is null
            ? null
            : new ResultadoRegistrarJuego { Filas = filasResultado, Numeros = numerosResultado };
        return View(vista);
    }

    private async Task<RegistrarJuegoViewModel> ArmarAsync(RegistrarJuegoViewModel? publicado, CancellationToken cancellationToken)
    {
        var operativa = await _api.OperativaAsync(cancellationToken);
        var loterias = await _api.LoteriasTodasAsync(cancellationToken);
        var hoy = await _api.LoteriasAsync(cancellationToken);
        return new RegistrarJuegoViewModel
        {
            HorarioCerrado = HorarioCerrado(operativa.Data),
            Clave = string.IsNullOrWhiteSpace(publicado?.Clave) ? Guid.NewGuid().ToString("N") : publicado.Clave,
            VentaId = string.IsNullOrWhiteSpace(publicado?.VentaId) ? Guid.NewGuid().ToString("N") : publicado.VentaId,
            UsarFilas = publicado?.UsarFilas ?? false,
            UsarNumeros = publicado?.UsarNumeros ?? false,
            Loterias = (loterias.Data ?? []).OrderBy(l => l.Nombre).ToList(),
            LoteriasHoy = (hoy.Data ?? []).OrderBy(l => l.Nombre).ToList(),
            Restringidos = operativa.Data?.NumerosRestringidos?.ToList() ?? [],
            Filas = publicado?.Filas is { Count: > 0 } filas ? filas : [new FilaJuegoForm()],
            LoteriasNumeros = publicado?.LoteriasNumeros ?? [],
            ValorJugado = publicado?.ValorJugado ?? string.Empty,
            Numeros = publicado?.Numeros is { Count: > 0 } numeros ? numeros : [string.Empty],
            Resultado = publicado?.Resultado
        };
    }

    private static string MensajeDecision(DecisionRegistrarJuego decision, bool horarioCerrado, bool usarFilas, bool usarNumeros)
    {
        if (horarioCerrado)
        {
            return UiTexts.JuegosCerrados;
        }

        var faltaFilas = usarFilas && decision.Filas != ParteJuego.Lista;
        var faltaNumeros = usarNumeros && decision.Numeros != ParteJuego.Lista;
        if (faltaFilas && faltaNumeros)
        {
            return UiTexts.CompleteAmbos;
        }

        if (faltaFilas)
        {
            return UiTexts.CompleteFilas;
        }

        if (faltaNumeros && decision.Numeros == ParteJuego.Parcial)
        {
            return decision.Motivo switch
            {
                MotivoJuegoNumeros.Loterias => JuegoNumerosManualMessages.LoteriasRequeridas,
                MotivoJuegoNumeros.Valor => JuegoNumerosManualMessages.ValorRequerido,
                MotivoJuegoNumeros.Numeros => JuegoNumerosManualMessages.NumerosRequeridos,
                MotivoJuegoNumeros.Formato => string.Format(JuegoNumerosManualMessages.NumeroFormato, decision.Detalle),
                MotivoJuegoNumeros.Repetido => string.Format(JuegoNumerosManualMessages.NumeroRepetido, decision.Detalle),
                MotivoJuegoNumeros.Restringido => string.Format(JuegoNumerosManualMessages.NumeroRestringido, decision.Detalle),
                MotivoJuegoNumeros.Insuficiente => string.Format(JuegoNumerosManualMessages.ValorInsuficiente, decision.Apuestas),
                _ => UiTexts.CompleteNumeros
            };
        }

        if (faltaNumeros)
        {
            return UiTexts.CompleteNumeros;
        }

        return UiTexts.CompleteParaRegistrar;
    }

    private static bool Pesos(string? texto, out decimal valor)
    {
        valor = 0;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var limpio = texto.Replace(".", string.Empty).Trim();
        return limpio.Length > 0
            && limpio.All(char.IsDigit)
            && decimal.TryParse(limpio, NumberStyles.None, CultureInfo.InvariantCulture, out valor);
    }
}

public sealed class HistoricoController : VendedorController
{
    private readonly IVendedorApiClient _api;

    public HistoricoController(IVendedorApiClient api) => _api = api;

    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, int page = 1, int pageSize = 10, string? orden = null, string? dir = null, CancellationToken cancellationToken = default)
    {
        Nav("historico", UiTexts.Historico);
        var hoy = ZonaHorariaColombia.ALocal(DateTime.UtcNow).Date;
        var inicio = (desde ?? hoy.AddDays(-9)).Date;
        var fin = (hasta ?? hoy).Date;
        if (fin < inicio)
        {
            (inicio, fin) = (fin, inicio);
        }

        var registros = await _api.ConsultarRegistrosAsync(inicio, fin, cancellationToken);
        var negado = SiNoAutorizado(registros);
        if (negado is not null)
        {
            return negado;
        }

        if (!registros.Success)
        {
            Aviso(registros.Message);
        }

        var columna = HistoricoOrden.Columna(orden);
        var direccion = HistoricoOrden.Direccion(orden, dir);
        var filas = (registros.Data ?? []).Select(f => new HistoricoFilaViewModel
        {
            Fecha = f.FechaRegistro,
            Codigo = f.CodigoPublico,
            Loteria = f.Loteria,
            Numero = f.Numero,
            Valor = f.Valor,
            Tipo = f.Tipo
        });
        var ordenadas = HistoricoOrden.Aplicar(filas, columna, direccion);
        var tamano = Paginas.Tamano(pageSize);
        var numero = Paginas.Numero(page, ordenadas.Count, tamano);
        return View(new HistoricoViewModel
        {
            Desde = inicio,
            Hasta = fin,
            Page = numero,
            PageSize = tamano,
            Total = ordenadas.Count,
            Orden = columna,
            Direccion = direccion,
            Items = ordenadas.Skip((numero - 1) * tamano).Take(tamano).ToList()
        });
    }
}

public sealed class AlertasController : VendedorController
{
    private readonly IVendedorApiClient _api;

    public AlertasController(IVendedorApiClient api) => _api = api;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        Nav("alertas", UiTexts.Alertas);
        var operativa = await _api.OperativaAsync(cancellationToken);
        var alertas = await _api.AlertasAsync(cancellationToken);
        var negado = SiNoAutorizado(alertas);
        if (negado is not null)
        {
            return negado;
        }

        if (alertas.Success && (alertas.Data?.Pendientes ?? 0) > 0)
        {
            await _api.MarcarAlertasLeidasAsync(cancellationToken);
        }

        return View(new AlertasViewModel
        {
            HorarioCerrado = HorarioCerrado(operativa.Data),
            NumerosRestringidos = operativa.Data?.NumerosRestringidos ?? [],
            Items = alertas.Data?.Items ?? []
        });
    }
}

public sealed class ValidarController : VendedorController
{
    private readonly IVendedorApiClient _api;

    public ValidarController(IVendedorApiClient api) => _api = api;

    [HttpGet]
    public IActionResult Index()
    {
        Nav("validar", UiTexts.ValidarTicket);
        return View(new ValidarViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ValidarViewModel model, CancellationToken cancellationToken)
    {
        Nav("validar", UiTexts.ValidarTicket);
        if (string.IsNullOrWhiteSpace(model.Codigo))
        {
            ModelState.AddModelError(nameof(model.Codigo), ValidationMessages.CampoRequerido);
            return View(model);
        }

        var result = await _api.ConsultarTicketAsync(model.Codigo.Trim(), cancellationToken);
        var negado = SiNoAutorizado(result);
        if (negado is not null)
        {
            return negado;
        }

        model.Resultado = result.Data?.ResultadoVisual;
        model.Mensaje = result.Success ? result.Data?.Mensaje : result.Message;
        model.Tono = result.Data?.Tono;
        return View(model);
    }
}

public sealed class ResultadosController : VendedorController
{
    private readonly IVendedorApiClient _api;

    public ResultadosController(IVendedorApiClient api) => _api = api;

    public async Task<IActionResult> Index(DateOnly? fecha, Guid? loteriaId, int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        Nav("resultados", UiTexts.Resultados);
        var resultados = await _api.ResultadosAsync(fecha, loteriaId, cancellationToken);
        var loterias = await _api.LoteriasAsync(cancellationToken);
        var negado = SiNoAutorizado(resultados);
        if (negado is not null)
        {
            return negado;
        }

        if (!resultados.Success)
        {
            Aviso(resultados.Message);
        }

        var tamano = Paginas.Tamano(pageSize);
        var lista = resultados.Data ?? [];
        var numero = Paginas.Numero(page, lista.Count, tamano);
        return View(new ResultadosViewModel
        {
            Fecha = fecha,
            LoteriaId = loteriaId,
            Page = numero,
            PageSize = tamano,
            Total = lista.Count,
            Loterias = loterias.Data ?? [],
            Items = lista.Skip((numero - 1) * tamano).Take(tamano).ToList()
        });
    }
}

public sealed class SoporteController : VendedorController
{
    private const long TamanoMaximoEnvio = 6_000_000;
    private readonly IVendedorApiClient _api;
    private readonly IConfiguration _config;

    public SoporteController(IVendedorApiClient api, IConfiguration config)
    {
        _api = api;
        _config = config;
    }

    public async Task<IActionResult> Index(Guid? id, CancellationToken cancellationToken)
    {
        Nav("soporte", UiTexts.Atencion);
        var lista = await _api.ConversacionesAsync(cancellationToken);
        var negado = SiNoAutorizado(lista);
        if (negado is not null)
        {
            return negado;
        }

        if (!lista.Success)
        {
            Aviso(lista.Message);
        }

        var conversaciones = lista.Data ?? [];
        var seleccionada = id ?? conversaciones.FirstOrDefault()?.ConversacionId;
        ConversacionDetalleResponse? hilo = null;
        if (seleccionada.HasValue)
        {
            var detalle = await _api.ConversacionAsync(seleccionada.Value, cancellationToken);
            negado = SiNoAutorizado(detalle);
            if (negado is not null)
            {
                return negado;
            }

            hilo = detalle.Data;
            if (hilo is not null)
            {
                await _api.MarcarChatLeidoAsync(seleccionada.Value, cancellationToken);
            }
        }

        return View("Index", new SoporteViewModel
        {
            Conversaciones = conversaciones,
            ConversacionId = hilo is null ? null : seleccionada,
            Hilo = hilo
        });
    }

    public IActionResult Tecnico() => RedirectToAction(nameof(Index));

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = TamanoMaximoEnvio)]
    [RequestSizeLimit(TamanoMaximoEnvio)]
    public async Task<IActionResult> Iniciar(string? texto, IFormFile? archivo, CancellationToken cancellationToken)
    {
        var request = new IniciarChatRequest { Texto = texto?.Trim() ?? string.Empty };
        (request.NombreArchivo, request.ContenidoBase64) = await LeerArchivoAsync(archivo, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Texto) && string.IsNullOrWhiteSpace(request.NombreArchivo))
        {
            Aviso(ChatMessages.TextoOAdjuntoRequerido);
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.IniciarConversacionAsync(request, cancellationToken);
        var negado = SiNoAutorizado(result);
        if (negado is not null)
        {
            return negado;
        }

        if (!result.Success || result.Data is null)
        {
            Aviso(result.Message);
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Index), new { id = result.Data.ConversacionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = TamanoMaximoEnvio)]
    [RequestSizeLimit(TamanoMaximoEnvio)]
    public async Task<IActionResult> Enviar(Guid id, string? texto, IFormFile? archivo, CancellationToken cancellationToken)
    {
        var request = new EnviarMensajeRequest { Texto = texto?.Trim() ?? string.Empty };
        (request.NombreArchivo, request.ContenidoBase64) = await LeerArchivoAsync(archivo, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Texto) && string.IsNullOrWhiteSpace(request.NombreArchivo))
        {
            Aviso(ChatMessages.TextoOAdjuntoRequerido);
            return RedirectToAction(nameof(Index), new { id });
        }

        var result = await _api.EnviarMensajeAsync(id, request, cancellationToken);
        var negado = SiNoAutorizado(result);
        if (negado is not null)
        {
            return negado;
        }

        if (!result.Success)
        {
            Aviso(result.Message);
        }

        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Adjunto(Guid id, CancellationToken cancellationToken)
    {
        var adjunto = await _api.DescargarAdjuntoAsync(id, cancellationToken);
        if (adjunto is null)
        {
            Aviso(ChatMessages.AdjuntoNoEncontrado);
            return RedirectToAction(nameof(Index));
        }

        return File(adjunto.Contenido, adjunto.Tipo, adjunto.Nombre);
    }

    [HttpGet]
    public IActionResult Conexion()
    {
        var token = Request.Cookies[AuthCookieNames.AccessToken];
        if (string.IsNullOrWhiteSpace(token))
        {
            return Unauthorized();
        }

        var baseUrl = _config["Api:BaseUrl"] ?? "http://localhost:8090/";
        var chatHubUrl = HubNotificacionesUrl.Resolver(baseUrl, Request.Host.Host, HubRutas.Chat);
        return Json(new { token, chatHubUrl });
    }

    private static async Task<(string? Nombre, string? Base64)> LeerArchivoAsync(IFormFile? archivo, CancellationToken cancellationToken)
    {
        if (archivo is not { Length: > 0 })
        {
            return (null, null);
        }

        await using var buffer = new MemoryStream();
        await archivo.CopyToAsync(buffer, cancellationToken);
        return (archivo.FileName, Convert.ToBase64String(buffer.ToArray()));
    }
}

public sealed class EntregaPremioController : VendedorController
{
    private const int FilasPremiosWeb = 10;
    private const int DigitosMaximosNumero = 4;
    private const int DigitosMaximosValor = 12;
    private const decimal ValorApostadoMaximo = 999_000m;
    private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");
    private readonly IVendedorApiClient _api;

    public EntregaPremioController(IVendedorApiClient api) => _api = api;

    public Task<IActionResult> Index(int page = 1, int pageSize = 10, int pageW = 1, CancellationToken cancellationToken = default)
    {
        Nav("entrega", UiTexts.EntregaPremio);
        return PaginaAsync(page, pageSize, pageW, null, null, cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cargar(CargarPremioViewModel model, int page = 1, int pageSize = 10, int pageW = 1, CancellationToken cancellationToken = default)
    {
        Nav("entrega", UiTexts.EntregaPremio);
        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        var numero = model.Numero?.Trim() ?? string.Empty;
        string? error = null;
        if (numero.Length is 0 or > DigitosMaximosNumero || !numero.All(char.IsAsciiDigit))
        {
            error = UiTexts.PremioNumeroInvalido;
        }
        else if (!Pesos(model.ValorApostado, out var apostado) || apostado > ValorApostadoMaximo)
        {
            error = UiTexts.PremioValorApostadoInvalido;
        }
        else if (model.LoteriaId == Guid.Empty)
        {
            error = UiTexts.PremioLoteriaObligatoria;
        }
        else if (model.FechaPremio is not { } fecha || fecha > hoy)
        {
            error = UiTexts.PremioFechaInvalida;
        }
        else if (!Pesos(model.ValorPremio, out var premio))
        {
            error = UiTexts.PremioValorInvalido;
        }
        else
        {
            var cargado = await _api.CargarPremioWebAsync(
                new RegistrarPremioWebRequest(numero, apostado, model.LoteriaId, fecha, premio, model.Password?.Trim()),
                cancellationToken);
            var negado = SiNoAutorizado(cargado);
            if (negado is not null)
            {
                return negado;
            }

            if (cargado.Success && cargado.Data is not null)
            {
                Aviso(string.Format(Colombia, UiTexts.PremioCargado, cargado.Data.Numero, cargado.Data.Loteria, Montos.De(cargado.Data.ValorPremio)));
                return RedirectToAction(nameof(Index), new { page, pageSize });
            }

            if (cargado.Message is PremioEntregadoWebMessages.ContrasenaIncorrecta or PremioEntregadoWebMessages.ContrasenaRequerida)
            {
                return await PaginaAsync(page, pageSize, pageW, model, cargado.Message, cancellationToken);
            }

            error = cargado.Message;
        }

        ViewData["AvisoPagina"] = error;
        return await PaginaAsync(page, pageSize, pageW, model, null, cancellationToken);
    }

    private async Task<IActionResult> PaginaAsync(
        int page,
        int pageSize,
        int pageW,
        CargarPremioViewModel? formulario,
        string? errorConfirmacion,
        CancellationToken cancellationToken)
    {
        var entregas = await _api.EntregasAsync(cancellationToken);
        var negado = SiNoAutorizado(entregas);
        if (negado is not null)
        {
            return negado;
        }

        if (!entregas.Success)
        {
            Aviso(entregas.Message);
        }

        var loterias = await _api.LoteriasTodasAsync(cancellationToken);
        negado = SiNoAutorizado(loterias);
        if (negado is not null)
        {
            return negado;
        }

        pageW = Math.Max(1, pageW);
        var premiosWeb = await _api.MisPremiosWebAsync(pageW, FilasPremiosWeb, cancellationToken);
        negado = SiNoAutorizado(premiosWeb);
        if (negado is not null)
        {
            return negado;
        }

        var hoy = DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));
        var tamano = Paginas.Tamano(pageSize);
        var lista = entregas.Data ?? [];
        var numero = Paginas.Numero(page, lista.Count, tamano);
        var totalWeb = premiosWeb.Data?.TotalFilas ?? 0;
        formulario ??= new CargarPremioViewModel { FechaPremio = hoy };
        formulario.Password = null;
        return View("Index", new EntregaPremioViewModel
        {
            Page = numero,
            PageSize = tamano,
            Total = lista.Count,
            Items = lista.Skip((numero - 1) * tamano).Take(tamano).ToList(),
            Hoy = hoy,
            Formulario = formulario,
            Loterias = (loterias.Data ?? []).Where(l => l.Estado == EstadoGeneral.Activo).OrderBy(l => l.Nombre).ToList(),
            AbrirConfirmacion = errorConfirmacion is not null,
            ErrorConfirmacion = errorConfirmacion,
            AvisoPremiosWeb = premiosWeb.Success ? null : premiosWeb.Message,
            PremiosWeb = (premiosWeb.Data?.Items ?? []).Select(p => new PremioWebFilaViewModel
            {
                Fecha = p.FechaPremio.ToString("dd-MM-yyyy"),
                Loteria = p.Loteria,
                Numero = p.Numero,
                ValorApostado = Montos.De(p.ValorApostado),
                ValorPremio = Montos.De(p.ValorPremio),
                Registrado = ZonaHorariaColombia.ALocal(p.FechaRegistro).ToString("dd-MM-yyyy hh:mm tt", Colombia)
            }).ToList(),
            PaginacionWeb = new PaginacionViewModel
            {
                Page = Paginas.Numero(pageW, totalWeb, FilasPremiosWeb),
                PageSize = FilasPremiosWeb,
                Total = totalWeb,
                Accion = nameof(Index),
                Controlador = "EntregaPremio"
            }
        });
    }

    private static bool Pesos(string? texto, out decimal valor)
    {
        valor = 0;
        var limpio = (texto ?? string.Empty).Replace(".", string.Empty).Trim();
        return limpio.Length is > 0 and <= DigitosMaximosValor
            && limpio.All(char.IsAsciiDigit)
            && decimal.TryParse(limpio, NumberStyles.None, CultureInfo.InvariantCulture, out valor)
            && valor > 0;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(Guid id, decimal valorGanado, CancellationToken cancellationToken)
    {
        var result = await _api.RegistrarEntregaAsync(id, valorGanado, cancellationToken);
        Aviso(result.Success ? SuccessMessages.EntregaPremioRegistrada : result.Message);
        return RedirectToAction(nameof(Index));
    }
}
