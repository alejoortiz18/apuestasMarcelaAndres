using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.JuegoNumerosManual;
using NewRich.Application.Contracts.Ventas;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IVentaManualService
{
    Task<Result<VentaResponse>> ConfirmarAsync(Guid vendedorId, ConfirmarVentaRequest request, string? idempotencyKey, CancellationToken cancellationToken, bool aplicarLimiteLineas = true);
    Task<Result<VentaResponse>> RegistrarNumerosAsync(Guid vendedorId, RegistrarJuegoNumerosManualRequest request, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<VentaResponse>>> ConsultarAsync(Guid vendedorId, ConsultaVentasRequest request, CancellationToken cancellationToken);
    Task<Result<ConsultaTicketResponse>> ConsultarCodigoAsync(Guid vendedorId, string? codigo, CancellationToken cancellationToken);
}

public sealed class VentaManualService : IVentaManualService
{
    private readonly INewRichDbContext _db;
    private readonly IVentaManualRepositorio _ventas;
    private readonly IClock _clock;

    public VentaManualService(INewRichDbContext db, IVentaManualRepositorio ventas, IClock clock)
    {
        _db = db;
        _ventas = ventas;
        _clock = clock;
    }

    public async Task<Result<VentaResponse>> ConfirmarAsync(
        Guid vendedorId,
        ConfirmarVentaRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken,
        bool aplicarLimiteLineas = true)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result<VentaResponse>.Fail(ValidationMessages.IdempotencyKeyRequerida);
        }

        var existente = await _ventas.BuscarPorIdempotenciaAsync(idempotencyKey, cancellationToken);
        if (existente is not null)
        {
            return Result<VentaResponse>.Ok(await MapAsync(existente, cancellationToken), VentaMessages.VentaDuplicadaIdempotente);
        }

        if (request.Juegos is null || request.Juegos.Count == 0)
        {
            return Result<VentaResponse>.Fail(VentaMessages.VentaSinLineas);
        }

        if (aplicarLimiteLineas)
        {
            var maximo = await _db.ConfiguracionesTipoApuesta
                .FirstOrDefaultAsync(x => x.TipoApuesta == request.TipoApuesta.ToString(), cancellationToken);
            if (maximo is not null && request.Juegos.Count > maximo.Maximo)
            {
                return Result<VentaResponse>.Fail(VentaMessages.MaximoLineasExcedido);
            }
        }

        if (request.TipoApuesta == TipoApuesta.INDIVIDUAL
            && request.Juegos.Any(j => (j.LoteriaIds?.Distinct().Count() ?? 0) != 1))
        {
            return Result<VentaResponse>.Fail(VentaMessages.IndividualUnaLoteria);
        }

        try
        {
            var vendedor = await _db.Usuarios.FirstAsync(u => u.UsuarioId == vendedorId, cancellationToken);
            var vigencia = await ObtenerVigencia(cancellationToken);
            var restringidos = await _db.NumerosRestringidos.Select(x => x.Numero).ToListAsync(cancellationToken);
            var venta = new VentaManual
            {
                VentaId = Guid.NewGuid(),
                UsuarioId = vendedorId,
                FechaVenta = _clock.LocalNow,
                TipoApuesta = request.TipoApuesta,
                IdempotencyKey = idempotencyKey
            };
            var boleto = new BoletoManual
            {
                BoletoId = Guid.NewGuid(),
                VentaId = venta.VentaId,
                EstadoBoleto = "Jugado",
                FechaCreacion = _clock.LocalNow,
                VigenciaDias = vigencia
            };
            decimal total = 0;
            foreach (var linea in request.Juegos)
            {
                if (!NumeroApuesta.EsValido(linea.Numero))
                {
                    throw new InvalidOperationException(ValidationMessages.NumeroApuestaFormato);
                }

                if (NumerosRestringidos.EstaBloqueado(linea.Numero, restringidos))
                {
                    throw new InvalidOperationException(string.Format(VentaMessages.NumeroRestringido, linea.Numero.Trim()));
                }

                if (linea.Valor <= 0)
                {
                    throw new InvalidOperationException(ValidationMessages.ValorApuestaMayorCero);
                }

                if (linea.LoteriaIds is null || linea.LoteriaIds.Count == 0)
                {
                    throw new InvalidOperationException(ValidationMessages.LoteriasRequeridas);
                }

                var loterias = await _db.Loterias.Where(l => linea.LoteriaIds.Contains(l.LoteriaId)).ToListAsync(cancellationToken);
                if (loterias.Count != linea.LoteriaIds.Distinct().Count())
                {
                    throw new InvalidOperationException(VentaMessages.LoteriaNoEncontrada);
                }

                if (loterias.Any(l => l.Estado != EstadoGeneral.Activo))
                {
                    throw new InvalidOperationException(VentaMessages.LoteriaInactiva);
                }

                if (loterias.Any(l => !ValidacionTope.LoteriaJugable(l.Tope)))
                {
                    throw new InvalidOperationException(
                        $"La lotería {loterias.First(l => !ValidacionTope.LoteriaJugable(l.Tope)).Nombre} no se puede utilizar (tope en cero).");
                }

                var hoy = DiasVentaLoteria.DiaDe(_clock.LocalNow);
                var loteriaIds = loterias.Select(l => l.LoteriaId).ToList();
                var dias = await _db.LoteriasDiasSemana.Where(d => loteriaIds.Contains(d.LoteriaId)).ToListAsync(cancellationToken);
                var porLoteria = dias.GroupBy(d => d.LoteriaId).ToDictionary(g => g.Key, g => g.Select(x => x.DiaSemana).ToList());
                if (loterias.Any(l => !DiasVentaLoteria.SePuedeVender(EstadoGeneral.Activo, porLoteria.GetValueOrDefault(l.LoteriaId), hoy)))
                {
                    throw new InvalidOperationException(VentaMessages.LoteriaNoHabilitadaHoy);
                }

                var juego = new JuegoManual
                {
                    JuegoId = Guid.NewGuid(),
                    BoletoId = boleto.BoletoId,
                    Numero = linea.Numero.Trim(),
                    Valor = linea.Valor,
                    TipoJuego = request.TipoApuesta == TipoApuesta.COMBINADO ? TipoJuego.COMBINADA : TipoJuego.INDIVIDUAL
                };
                foreach (var loteria in loterias)
                {
                    juego.Loterias.Add(new JuegoLoteriaManual
                    {
                        JuegoId = juego.JuegoId,
                        LoteriaId = loteria.LoteriaId,
                        NombreLoteria = loteria.Nombre
                    });
                }

                total += TotalesApuesta.TotalJuego(linea.Valor, loterias.Count);
                boleto.Juegos.Add(juego);
            }

            venta.Total = total;
            boleto.CodigoPublico = await GenerarCodigoPublico(cancellationToken);
            venta.Boletos.Add(boleto);
            await _ventas.GuardarAsync(venta, cancellationToken);
            var respuesta = await MapAsync(venta, cancellationToken);
            respuesta.Vendedor = vendedor.Alias ?? vendedor.NombreCompleto;
            return Result<VentaResponse>.Created(respuesta, SuccessMessages.VentaConfirmada);
        }
        catch (InvalidOperationException ex)
        {
            return Result<VentaResponse>.Fail(ex.Message);
        }
    }

    public async Task<Result<VentaResponse>> RegistrarNumerosAsync(
        Guid vendedorId,
        RegistrarJuegoNumerosManualRequest request,
        CancellationToken cancellationToken)
    {
        if (request.VentaId == Guid.Empty)
        {
            return Result<VentaResponse>.Fail(JuegoNumerosManualMessages.VentaIdRequerido);
        }

        if (request.LoteriaIds is null || request.LoteriaIds.Count == 0)
        {
            return Result<VentaResponse>.Fail(JuegoNumerosManualMessages.LoteriasRequeridas);
        }

        if (request.LoteriaIds.Distinct().Count() != request.LoteriaIds.Count)
        {
            return Result<VentaResponse>.Fail(JuegoNumerosManualMessages.LoteriaRepetida);
        }

        if (request.ValorJugado != decimal.Truncate(request.ValorJugado) || request.ValorJugado <= 0)
        {
            return Result<VentaResponse>.Fail(JuegoNumerosManualMessages.ValorEntero);
        }

        var numeros = (request.Numeros ?? []).Select(n => n.Trim()).Where(n => n.Length > 0).ToList();
        if (numeros.Count == 0)
        {
            return Result<VentaResponse>.Fail(JuegoNumerosManualMessages.NumerosRequeridos);
        }

        var vistos = new HashSet<string>();
        var restringidos = await _db.NumerosRestringidos.Select(x => x.Numero).ToListAsync(cancellationToken);
        foreach (var numero in numeros)
        {
            if (!NumeroApuesta.EsValido(numero))
            {
                return Result<VentaResponse>.Fail(string.Format(JuegoNumerosManualMessages.NumeroFormato, numero));
            }

            if (!vistos.Add(numero))
            {
                return Result<VentaResponse>.Fail(string.Format(JuegoNumerosManualMessages.NumeroRepetido, numero));
            }

            if (NumerosRestringidos.EstaBloqueado(numero, restringidos))
            {
                return Result<VentaResponse>.Fail(string.Format(JuegoNumerosManualMessages.NumeroRestringido, numero));
            }
        }

        var apuestas = RepartoJuegoNumerosManual.CantidadApuestas(numeros.Count, request.LoteriaIds.Count);
        if (!RepartoJuegoNumerosManual.AlcanzaParaTodas(request.ValorJugado, apuestas))
        {
            return Result<VentaResponse>.Fail(string.Format(JuegoNumerosManualMessages.ValorInsuficiente, apuestas));
        }

        var reparto = RepartoJuegoNumerosManual.Repartir(request.ValorJugado, numeros, request.LoteriaIds);
        return await ConfirmarAsync(
            vendedorId,
            new ConfirmarVentaRequest
            {
                TipoApuesta = TipoApuesta.INDIVIDUAL,
                Juegos = reparto.Select(a => new LineaJuegoRequest
                {
                    Numero = a.Numero,
                    Valor = a.Valor,
                    LoteriaIds = [a.LoteriaId]
                }).ToList()
            },
            request.VentaId.ToString("N"),
            cancellationToken,
            aplicarLimiteLineas: false);
    }

    public async Task<Result<IReadOnlyList<VentaResponse>>> ConsultarAsync(
        Guid vendedorId,
        ConsultaVentasRequest request,
        CancellationToken cancellationToken)
    {
        var desde = request.FechaInicial ?? _clock.LocalNow.Date;
        var hasta = request.FechaFinal ?? desde.Date.AddDays(1).AddTicks(-1);
        var ventas = await _ventas.ConsultarAsync(vendedorId, desde, hasta, cancellationToken);
        var lista = new List<VentaResponse>();
        foreach (var venta in ventas)
        {
            lista.Add(await MapAsync(venta, cancellationToken));
        }

        return Result<IReadOnlyList<VentaResponse>>.Ok(lista, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<ConsultaTicketResponse>> ConsultarCodigoAsync(
        Guid vendedorId,
        string? codigo,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            return Result<ConsultaTicketResponse>.Fail(BoletoMessages.BoletoNoEncontrado, 404);
        }

        var venta = await _ventas.BuscarPorCodigoAsync(vendedorId, codigo.Trim(), cancellationToken);
        if (venta is null)
        {
            return Result<ConsultaTicketResponse>.Fail(BoletoMessages.BoletoNoEncontrado, 404);
        }

        var boleto = venta.Boletos.First();
        return Result<ConsultaTicketResponse>.Ok(new ConsultaTicketResponse
        {
            ResultadoVisual = BoletoMessages.BoletoJugado,
            Mensaje = string.Join(", ", boleto.Juegos.Select(j => j.Numero.Trim())),
            Tono = "jugado",
            BoletoId = boleto.BoletoId
        }, SuccessMessages.OperacionExitosa);
    }

    private async Task<VentaResponse> MapAsync(VentaManual venta, CancellationToken cancellationToken)
    {
        var boleto = venta.Boletos.First();
        var ids = boleto.Juegos.SelectMany(j => j.Loterias).Select(l => l.LoteriaId).Distinct().ToList();
        var nombres = await _db.Loterias
            .Where(l => ids.Contains(l.LoteriaId))
            .ToDictionaryAsync(l => l.LoteriaId, l => l.Nombre, cancellationToken);
        var vendedor = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == venta.UsuarioId, cancellationToken);
        return new VentaResponse
        {
            VentaId = venta.VentaId,
            BoletoId = boleto.BoletoId,
            CodigoPublico = boleto.CodigoPublico.Trim(),
            CodigoImpreso = CodigoPublicoGenerator.FormatoImpreso(boleto.CodigoPublico.Trim()),
            Qr = string.Empty,
            VendedorId = venta.UsuarioId,
            Vendedor = vendedor?.Alias ?? vendedor?.NombreCompleto ?? string.Empty,
            FechaVenta = venta.FechaVenta,
            Total = venta.Total,
            EstadoBoleto = boleto.EstadoBoleto,
            TipoApuesta = venta.TipoApuesta,
            Juegos = boleto.Juegos.Select(j => new JuegoResponse
            {
                JuegoId = j.JuegoId,
                Numero = j.Numero.Trim(),
                Valor = j.Valor,
                Total = TotalesApuesta.TotalJuego(j.Valor, j.Loterias.Count),
                Loterias = j.Loterias.Select(l => l.NombreLoteria ?? nombres.GetValueOrDefault(l.LoteriaId) ?? string.Empty).ToList()
            }).ToList()
        };
    }

    private async Task<string> GenerarCodigoPublico(CancellationToken cancellationToken)
    {
        for (var i = 0; i < 20; i++)
        {
            var codigo = CodigoPublicoGenerator.Formatear(Random.Shared.Next(0, 10_000_000));
            if (!await _ventas.ExisteCodigoAsync(codigo, cancellationToken))
            {
                return codigo;
            }
        }

        throw new InvalidOperationException(VentaMessages.CodigoPublicoNoGenerado);
    }

    private async Task<int> ObtenerVigencia(CancellationToken cancellationToken)
    {
        var item = await _db.Configuraciones.FirstOrDefaultAsync(c => c.Clave == "VigenciaPremiosDias", cancellationToken);
        return item is not null && int.TryParse(item.Valor, out var dias) ? dias : 30;
    }
}
