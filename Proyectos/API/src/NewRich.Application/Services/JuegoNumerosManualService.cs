using System.Globalization;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.JuegoNumerosManual;
using NewRich.Application.Contracts.Offline;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IJuegoNumerosManualService
{
    Task<Result<JuegoNumerosManualResponse>> RegistrarAsync(
        Guid vendedorId,
        Guid? dispositivoId,
        RegistrarJuegoNumerosManualRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// «Juego números» del PDA. La jugada queda como una venta INDIVIDUAL en las tablas actuales
/// (un juego por número en cada lotería), con el mismo código, clave de validación y QR de las
/// demás ventas, para que se valide como ganador igual que cualquier boleto. No valida topes,
/// pero sus apuestas suman a los acumulados. La marca de «Juego números» va en el esquema manual.
/// </summary>
public sealed class JuegoNumerosManualService : IJuegoNumerosManualService
{
    public const string PrefijoIdempotencia = "juego-numeros:";

    private readonly INewRichDbContext _db;
    private readonly IQrCryptoService _qr;
    private readonly IJuegoNumerosManualRepositorio _juegos;
    private readonly INotificacionService _notificaciones;
    private readonly IRecaudoService? _recaudo;

    public JuegoNumerosManualService(
        INewRichDbContext db,
        IQrCryptoService qr,
        IJuegoNumerosManualRepositorio juegos,
        INotificacionService notificaciones,
        IRecaudoService? recaudo = null)
    {
        _db = db;
        _qr = qr;
        _juegos = juegos;
        _notificaciones = notificaciones;
        _recaudo = recaudo;
    }

    public async Task<Result<JuegoNumerosManualResponse>> RegistrarAsync(
        Guid vendedorId,
        Guid? dispositivoId,
        RegistrarJuegoNumerosManualRequest request,
        CancellationToken cancellationToken)
    {
        if (request.VentaId == Guid.Empty)
        {
            return Fallo(JuegoNumerosManualMessages.VentaIdRequerido);
        }

        var clave = PrefijoIdempotencia + request.VentaId.ToString("N");
        var existente = await CargarVentaAsync(v => v.IdempotencyKey == clave, cancellationToken);
        if (existente is not null)
        {
            return existente.UsuarioId == vendedorId
                ? Result<JuegoNumerosManualResponse>.Ok(Mapear(existente), JuegoNumerosManualMessages.YaRegistrada)
                : Fallo(JuegoNumerosManualMessages.VentaIdUsado);
        }

        var fecha = await _juegos.FechaHoraColombiaAsync(cancellationToken);
        var validacion = await ValidarAsync(fecha, request, cancellationToken);
        if (validacion.Error is not null)
        {
            return Fallo(validacion.Error);
        }

        var numeros = validacion.Numeros;
        var loterias = validacion.Loterias;
        var avisos = new List<(IReadOnlyList<Guid> Destinatarios, string Tipo, string Mensaje, Guid VentaId, Guid JuegoId)>();
        Venta? venta = null;
        try
        {
            await _db.ExecuteInTransactionAsync(async ct =>
            {
                var vigencia = await ObtenerEntero("VigenciaPremiosDias", 30, ct);
                var alertaRepeticion = await ObtenerEntero("AlertaRepeticionNumero", 10, ct);
                var alertaValor = await ObtenerEntero("AlertaValorMinimo", 10000, ct);
                var admins = await _db.Usuarios
                    .Where(u => u.Rol == RolUsuario.Administrador && u.Estado == EstadoUsuario.Activo)
                    .Select(u => u.UsuarioId)
                    .ToListAsync(ct);

                venta = new Venta
                {
                    VentaId = Guid.NewGuid(),
                    UsuarioId = vendedorId,
                    DispositivoId = dispositivoId,
                    FechaVenta = fecha,
                    TipoApuesta = TipoApuesta.INDIVIDUAL,
                    EstadoSincronizacion = "Online",
                    IdempotencyKey = clave,
                    FechaSincronizacion = fecha,
                    Total = request.ValorJugado
                };

                var boleto = new Boleto
                {
                    BoletoId = Guid.NewGuid(),
                    VentaId = venta.VentaId,
                    EstadoBoleto = EstadoBoleto.Jugado,
                    FechaCreacion = fecha,
                    VigenciaDias = vigencia
                };

                var porId = loterias.ToDictionary(l => l.LoteriaId);
                var repeticionesHoy = await _db.Juegos
                    .Where(j => numeros.Contains(j.Numero) && j.Boleto!.FechaCreacion.Date == fecha.Date)
                    .GroupBy(j => j.Numero)
                    .Select(g => new { Numero = g.Key, Cantidad = g.Count() })
                    .ToDictionaryAsync(x => x.Numero.Trim(), x => x.Cantidad, ct);
                var avisadas = new HashSet<string>();
                foreach (var apuesta in RepartoJuegoNumerosManual.Repartir(request.ValorJugado, numeros, loterias.Select(l => l.LoteriaId).ToList()))
                {
                    var loteria = porId[apuesta.LoteriaId];
                    var juego = new Juego
                    {
                        JuegoId = Guid.NewGuid(),
                        BoletoId = boleto.BoletoId,
                        Numero = apuesta.Numero,
                        Valor = apuesta.Valor,
                        TipoJuego = TipoJuego.INDIVIDUAL
                    };
                    juego.JuegoLoterias.Add(new JuegoLoteria { JuegoId = juego.JuegoId, LoteriaId = loteria.LoteriaId, Loteria = loteria });
                    boleto.Juegos.Add(juego);

                    if (repeticionesHoy.GetValueOrDefault(apuesta.Numero) + loterias.Count >= alertaRepeticion && avisadas.Add(apuesta.Numero))
                    {
                        avisos.Add((admins, NotificacionMessages.TipoRepeticionNumero,
                            string.Format(VentaMessages.AlertaRepeticionNumero, apuesta.Numero), venta.VentaId, juego.JuegoId));
                    }

                    if (apuesta.Valor >= alertaValor)
                    {
                        avisos.Add((admins, NotificacionMessages.TipoValorAlto, string.Format(
                            VentaMessages.AlertaValorAlto,
                            apuesta.Valor.ToString("N0", CultureInfo.GetCultureInfo("es-CO")),
                            apuesta.Numero,
                            loteria.Nombre), venta.VentaId, juego.JuegoId));
                    }
                }

                boleto.CodigoPublico = await GenerarCodigoPublico(ct);
                var claveValidacion = _qr.GenerarClaveValidacion();
                boleto.ClaveValidacionHash = _qr.HashClaveValidacion(claveValidacion);
                var claveEntity = new ClaveValidacionBoleto
                {
                    ClaveId = Guid.NewGuid(),
                    BoletoId = boleto.BoletoId,
                    ClaveHash = boleto.ClaveValidacionHash,
                    Version = 1,
                    IdentificadorClave = Guid.NewGuid(),
                    FechaCreacion = fecha
                };
                boleto.QrCifrado = _qr.Encrypt(new QrPayload(
                    boleto.BoletoId, boleto.CodigoPublico, claveValidacion, 1, claveEntity.IdentificadorClave));

                venta.Boletos.Add(boleto);
                _db.Ventas.Add(venta);
                _db.ClavesValidacionBoleto.Add(claveEntity);
                await _db.SaveChangesAsync(ct);

                await _juegos.MarcarAsync(new MarcaJuegoNumerosManual(
                    venta.VentaId,
                    request.ValorJugado,
                    numeros.Count,
                    loterias.Count,
                    boleto.Juegos.Count,
                    fecha), ct);
            }, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Fallo(ex.Message);
        }

        foreach (var aviso in avisos)
        {
            await _notificaciones.CrearParaAsync(aviso.Destinatarios, aviso.Tipo, aviso.Mensaje, cancellationToken, aviso.VentaId, aviso.JuegoId);
        }

        if (_recaudo is not null)
        {
            try
            {
                await _recaudo.GenerarDesdeVentaAsync(vendedorId, cancellationToken);
            }
            catch (Exception)
            {
                // La venta ya quedó guardada. El recaudo se reintenta en la siguiente consulta.
            }
        }

        return Result<JuegoNumerosManualResponse>.Created(Mapear(venta!), JuegoNumerosManualMessages.Registrada);
    }

    private sealed record Validacion(string? Error, List<string> Numeros, List<Loteria> Loterias);

    private async Task<Validacion> ValidarAsync(DateTime fecha, RegistrarJuegoNumerosManualRequest request, CancellationToken cancellationToken)
    {
        static Validacion Error(string mensaje) => new(mensaje, [], []);

        var loteriaIds = request.LoteriaIds ?? [];
        if (loteriaIds.Count == 0)
        {
            return Error(JuegoNumerosManualMessages.LoteriasRequeridas);
        }

        if (loteriaIds.Distinct().Count() != loteriaIds.Count)
        {
            return Error(JuegoNumerosManualMessages.LoteriaRepetida);
        }

        if (request.ValorJugado <= 0)
        {
            return Error(JuegoNumerosManualMessages.ValorRequerido);
        }

        if (request.ValorJugado != decimal.Truncate(request.ValorJugado))
        {
            return Error(JuegoNumerosManualMessages.ValorEntero);
        }

        var numeros = (request.Numeros ?? []).Select(n => (n ?? string.Empty).Trim()).ToList();
        if (numeros.Count == 0)
        {
            return Error(JuegoNumerosManualMessages.NumerosRequeridos);
        }

        var restringidos = await _db.NumerosRestringidos.AsNoTracking().Select(x => x.Numero).ToListAsync(cancellationToken);
        var vistos = new HashSet<string>();
        foreach (var numero in numeros)
        {
            if (!NumeroApuesta.EsValido(numero))
            {
                return Error(string.Format(JuegoNumerosManualMessages.NumeroFormato, numero));
            }

            if (!vistos.Add(numero))
            {
                return Error(string.Format(JuegoNumerosManualMessages.NumeroRepetido, numero));
            }

            if (NumerosRestringidos.EstaBloqueado(numero, restringidos))
            {
                return Error(string.Format(JuegoNumerosManualMessages.NumeroRestringido, numero));
            }
        }

        var cantidad = RepartoJuegoNumerosManual.CantidadApuestas(numeros.Count, loteriaIds.Count);
        if (!RepartoJuegoNumerosManual.AlcanzaParaTodas(request.ValorJugado, cantidad))
        {
            return Error(string.Format(JuegoNumerosManualMessages.ValorInsuficiente, cantidad));
        }

        var encontradas = await _db.Loterias
            .Where(l => loteriaIds.Contains(l.LoteriaId))
            .ToDictionaryAsync(l => l.LoteriaId, cancellationToken);
        if (encontradas.Count != loteriaIds.Count)
        {
            return Error(JuegoNumerosManualMessages.LoteriaNoEncontrada);
        }

        var dias = await _db.LoteriasDiasSemana.AsNoTracking()
            .Where(d => loteriaIds.Contains(d.LoteriaId))
            .ToListAsync(cancellationToken);
        var diasPorLoteria = dias.GroupBy(d => d.LoteriaId).ToDictionary(g => g.Key, g => g.Select(x => x.DiaSemana).ToList());
        var dia = DiasVentaLoteria.DiaDe(fecha);
        var loterias = loteriaIds.Select(id => encontradas[id]).ToList();
        foreach (var loteria in loterias)
        {
            if (loteria.Estado != EstadoGeneral.Activo)
            {
                return Error(string.Format(JuegoNumerosManualMessages.LoteriaInactiva, loteria.Nombre));
            }

            if (!ValidacionTope.LoteriaJugable(loteria.Tope))
            {
                return Error(string.Format(JuegoNumerosManualMessages.LoteriaTopeCero, loteria.Nombre));
            }

            if (!DiasVentaLoteria.SePuedeVender(EstadoGeneral.Activo, diasPorLoteria.GetValueOrDefault(loteria.LoteriaId), dia))
            {
                return Error(string.Format(JuegoNumerosManualMessages.LoteriaNoHabilitadaHoy, loteria.Nombre));
            }

            if (!HorarioLoteria.EstaVigente(fecha.TimeOfDay, loteria.HoraInicio, loteria.HoraFin))
            {
                return Error(string.Format(JuegoNumerosManualMessages.LoteriaCerrada, loteria.Nombre));
            }
        }

        return new Validacion(null, numeros, loterias);
    }

    private Task<Venta?> CargarVentaAsync(System.Linq.Expressions.Expression<Func<Venta, bool>> filtro, CancellationToken cancellationToken) =>
        _db.Ventas
            .Include(v => v.Boletos)
            .ThenInclude(b => b.Juegos)
            .ThenInclude(j => j.JuegoLoterias)
            .ThenInclude(jl => jl.Loteria)
            .FirstOrDefaultAsync(filtro, cancellationToken);

    /// <summary>Reconstruye la jugada desde los juegos del boleto: un juego por número en cada lotería.</summary>
    public static JuegoNumerosManualResponse Mapear(Venta venta)
    {
        var boleto = venta.Boletos.First();
        var juegos = boleto.Juegos.ToList();
        var apuestas = juegos.Select(j =>
        {
            var jl = j.JuegoLoterias.First();
            return new ApuestaJuegoNumerosManualResponse
            {
                Numero = j.Numero.Trim(),
                LoteriaId = jl.LoteriaId,
                Loteria = jl.Loteria?.Nombre ?? string.Empty,
                Valor = j.Valor
            };
        }).ToList();
        var numeros = apuestas.Select(a => a.Numero).Distinct().ToList();
        var loterias = apuestas.Select(a => a.Loteria).Distinct().ToList();
        return new JuegoNumerosManualResponse
        {
            VentaId = venta.VentaId,
            BoletoId = boleto.BoletoId,
            CodigoPublico = boleto.CodigoPublico,
            CodigoImpreso = CodigoPublicoGenerator.FormatoImpreso(boleto.CodigoPublico),
            Qr = SobreQrOfflineCodec.ParaPapel(boleto.QrCifrado, boleto.CodigoPublico),
            FechaVenta = venta.FechaVenta,
            ValorJugado = venta.Total,
            CantidadNumeros = numeros.Count,
            CantidadLoterias = loterias.Count,
            CantidadApuestas = apuestas.Count,
            ValorPorApuesta = RepartoJuegoNumerosManual.ValorBase(venta.Total, apuestas.Count),
            ApuestasConSobrante = RepartoJuegoNumerosManual.ApuestasConSobrante(venta.Total, apuestas.Count),
            Numeros = numeros,
            Loterias = loterias,
            Apuestas = apuestas
        };
    }

    private async Task<string> GenerarCodigoPublico(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 20; i++)
        {
            var codigo = CodigoPublicoGenerator.Formatear(Random.Shared.Next(0, 10_000_000));
            if (!await _db.Boletos.AnyAsync(b => b.CodigoPublico == codigo, cancellationToken))
            {
                return codigo;
            }
        }

        throw new InvalidOperationException(VentaMessages.CodigoPublicoNoGenerado);
    }

    private async Task<int> ObtenerEntero(string clave, int defecto, CancellationToken cancellationToken)
    {
        var item = await _db.Configuraciones.FirstOrDefaultAsync(c => c.Clave == clave, cancellationToken);
        return item is not null && int.TryParse(item.Valor, out var valor) ? valor : defecto;
    }

    private static Result<JuegoNumerosManualResponse> Fallo(string mensaje) =>
        Result<JuegoNumerosManualResponse>.Fail(mensaje);
}
