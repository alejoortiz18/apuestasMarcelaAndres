using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Contracts.JuegoNumerosManual;
using NewRich.Application.Contracts.Premios;
using NewRich.Application.Contracts.RegistrarJuegoManual;
using NewRich.Application.Contracts.Ventas;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Services;
using NewRich.Vendedor.Constants;
using NewRich.Vendedor.Models;
using NewRich.Vendedor.Services;

namespace NewRich.Vendedor.Controllers;

public sealed class InicioController : VendedorController
{
    private readonly IVendedorApiClient _api;

    public InicioController(IVendedorApiClient api) => _api = api;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        Nav("inicio", UiTexts.Inicio);
        var hoy = ZonaHorariaColombia.ALocal(DateTime.UtcNow).Date;
        var ventas = await _api.ConsultarVentasAsync(hoy, hoy, cancellationToken);
        var negado = SiNoAutorizado(ventas);
        if (negado is not null)
        {
            return negado;
        }

        var operativa = await _api.OperativaAsync(cancellationToken);
        var lista = ventas.Data ?? [];
        return View(new InicioViewModel
        {
            Total = Montos.De(lista.Sum(v => v.Total)),
            Boletos = lista.Count,
            HorarioCerrado = HorarioCerrado(operativa.Data),
            Aviso = ventas.Success ? null : ventas.Message
        });
    }
}

public sealed class VenderController : VendedorController
{
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
        var decision = EvaluacionRegistrarJuego.Evaluar(
            filas.Select(f => new FilaRegistroEntrada(f.LoteriaId != Guid.Empty, Pesos(f.Valor, out _), !string.IsNullOrWhiteSpace(f.Numero))).ToList(),
            loteriasMarcadas.Count,
            valorEscrito,
            valorJugado,
            numeros,
            operativo.Data?.NumerosRestringidos,
            horarioCerrado);

        if (!decision.PuedeRegistrar)
        {
            ModelState.AddModelError(string.Empty, MensajeDecision(decision, horarioCerrado));
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
            var result = await _api.RegistrarJuegoNumerosAsync(new RegistrarJuegoNumerosManualRequest
            {
                VentaId = ventaId,
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
                    Codigo = result.Data.CodigoImpreso,
                    Fecha = result.Data.FechaVenta.ToString("dd-MM-yyyy HH:mm:ss"),
                    Qr = QrImagen.DataUri(result.Data.Qr),
                    Loterias = result.Data.Loterias,
                    Numeros = result.Data.Numeros,
                    ValorJugado = Montos.De(result.Data.ValorJugado),
                    CantidadNumeros = result.Data.CantidadNumeros,
                    CantidadLoterias = result.Data.CantidadLoterias,
                    CantidadApuestas = result.Data.CantidadApuestas,
                    ValorPorApuesta = Montos.De(result.Data.ValorPorApuesta),
                    ApuestasConSobrante = result.Data.ApuestasConSobrante
                };
            }
        }

        var vista = await ArmarAsync(model, cancellationToken);
        if (!conservarFilas)
        {
            vista.Filas = [new FilaJuegoForm()];
            vista.Clave = Guid.NewGuid().ToString("N");
        }

        if (!conservarNumeros)
        {
            vista.LoteriasNumeros = [];
            vista.ValorJugado = string.Empty;
            vista.Numeros = [string.Empty];
            vista.VentaId = Guid.NewGuid().ToString("N");
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

    private static string MensajeDecision(DecisionRegistrarJuego decision, bool horarioCerrado)
    {
        if (horarioCerrado)
        {
            return UiTexts.JuegosCerrados;
        }

        if (decision.Filas == ParteJuego.Parcial && decision.Numeros == ParteJuego.Parcial)
        {
            return UiTexts.CompleteAmbos;
        }

        if (decision.Filas == ParteJuego.Parcial)
        {
            return UiTexts.CompleteFilas;
        }

        if (decision.Numeros == ParteJuego.Parcial)
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
                _ => UiTexts.CompleteParaRegistrar
            };
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
            Valor = f.Valor
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
    private readonly IVendedorApiClient _api;

    public SoporteController(IVendedorApiClient api) => _api = api;

    public Task<IActionResult> Index(Guid? id, CancellationToken cancellationToken) =>
        Vista(false, id, cancellationToken);

    public Task<IActionResult> Tecnico(Guid? id, CancellationToken cancellationToken) =>
        Vista(true, id, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Iniciar(string texto, CancellationToken cancellationToken)
    {
        var result = await _api.IniciarConversacionAsync(texto ?? string.Empty, cancellationToken);
        Aviso(result.Success ? result.Message : result.Message);
        return result.Success && result.Data is not null
            ? RedirectToAction(nameof(Index), new { id = result.Data.ConversacionId })
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enviar(Guid id, string texto, bool tecnico, CancellationToken cancellationToken)
    {
        var result = await _api.EnviarMensajeAsync(id, texto ?? string.Empty, cancellationToken);
        if (!result.Success)
        {
            Aviso(result.Message);
        }

        return tecnico
            ? RedirectToAction(nameof(Tecnico), new { id })
            : RedirectToAction(nameof(Index), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reportar(string observacion, string codigoTicket, CancellationToken cancellationToken)
    {
        var result = await _api.ReportarTecnicoAsync(observacion ?? string.Empty, codigoTicket ?? string.Empty, cancellationToken);
        Aviso(result.Success ? "El reporte técnico quedó enviado." : result.Message);
        return RedirectToAction(nameof(Tecnico));
    }

    private async Task<IActionResult> Vista(bool tecnico, Guid? id, CancellationToken cancellationToken)
    {
        Nav(tecnico ? "soporte-tecnico" : "soporte", tecnico ? UiTexts.SoporteTecnico : UiTexts.Atencion);
        var lista = await _api.ConversacionesAsync(tecnico, cancellationToken);
        var negado = SiNoAutorizado(lista);
        if (negado is not null)
        {
            return negado;
        }

        ConversacionDetalleResponse? hilo = null;
        if (id.HasValue)
        {
            var detalle = await _api.ConversacionAsync(id.Value, cancellationToken);
            hilo = detalle.Data;
        }

        return View("Index", new SoporteViewModel
        {
            Tecnico = tecnico,
            Conversaciones = lista.Data ?? [],
            Hilo = hilo
        });
    }
}

public sealed class EntregaPremioController : VendedorController
{
    private readonly IVendedorApiClient _api;

    public EntregaPremioController(IVendedorApiClient api) => _api = api;

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, CancellationToken cancellationToken = default)
    {
        Nav("entrega", UiTexts.EntregaPremio);
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

        var tamano = Paginas.Tamano(pageSize);
        var lista = entregas.Data ?? [];
        var numero = Paginas.Numero(page, lista.Count, tamano);
        return View(new EntregaPremioViewModel
        {
            Page = numero,
            PageSize = tamano,
            Total = lista.Count,
            Items = lista.Skip((numero - 1) * tamano).Take(tamano).ToList()
        });
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
