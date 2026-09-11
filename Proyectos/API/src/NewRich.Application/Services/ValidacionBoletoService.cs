using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Ventas;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class ValidacionBoletoService : IValidacionBoletoService
{
    private readonly INewRichDbContext _db;
    private readonly IQrCryptoService _qr;
    private readonly IClock _clock;

    public ValidacionBoletoService(INewRichDbContext db, IQrCryptoService qr, IClock clock)
    {
        _db = db;
        _qr = qr;
        _clock = clock;
    }

    public async Task<Result<ValidacionBoletoResponse>> ValidarQrAsync(ValidarQrRequest request, CancellationToken cancellationToken)
    {
        var payload = _qr.Decrypt(request.Qr);
        if (payload is null)
        {
            return Result<ValidacionBoletoResponse>.Ok(new ValidacionBoletoResponse { ResultadoVisual = BoletoMessages.BoletoNoEncontrado }, BoletoMessages.BoletoNoEncontrado);
        }

        var boleto = await QueryBoletos().FirstOrDefaultAsync(b => b.BoletoId == payload.BoletoId, cancellationToken);
        if (boleto is null || boleto.CodigoPublico != payload.CodigoPublico)
        {
            return Result<ValidacionBoletoResponse>.Ok(new ValidacionBoletoResponse { ResultadoVisual = BoletoMessages.BoletoNoEncontrado }, BoletoMessages.BoletoNoEncontrado);
        }

        var hash = _qr.HashClaveValidacion(payload.ClaveValidacion);
        if (!string.Equals(hash, boleto.ClaveValidacionHash, StringComparison.OrdinalIgnoreCase))
        {
            return Result<ValidacionBoletoResponse>.Ok(new ValidacionBoletoResponse { ResultadoVisual = BoletoMessages.BoletoNoEncontrado }, BoletoMessages.BoletoNoEncontrado);
        }

        var resultado = await Evaluar(boleto, cancellationToken);
        return Result<ValidacionBoletoResponse>.Ok(resultado, resultado.ResultadoVisual);
    }

    public async Task<Result<ValidacionBoletoResponse>> AutorizarPagoAsync(Guid boletoId, CancellationToken cancellationToken)
    {
        var boleto = await QueryBoletos().FirstOrDefaultAsync(b => b.BoletoId == boletoId, cancellationToken);
        if (boleto is null)
        {
            return Result<ValidacionBoletoResponse>.Fail(BoletoMessages.BoletoNoEncontrado, 404);
        }

        var evaluacion = await Evaluar(boleto, cancellationToken);
        if (evaluacion.ResultadoVisual == BoletoMessages.BoletoPagado)
        {
            return Result<ValidacionBoletoResponse>.Fail(BoletoMessages.BoletoYaCobrado);
        }

        if (evaluacion.ResultadoVisual != BoletoMessages.BoletoGanador)
        {
            return Result<ValidacionBoletoResponse>.Fail(BoletoMessages.BoletoNoEsGanador);
        }

        boleto.EstadoBoleto = EstadoBoleto.PagadoCobrado;
        await _db.SaveChangesAsync(cancellationToken);
        evaluacion.ResultadoVisual = BoletoMessages.BoletoPagado;
        evaluacion.Estado = BoletoMessages.BoletoPagado;
        return Result<ValidacionBoletoResponse>.Ok(evaluacion, SuccessMessages.BoletoValidado);
    }

    public async Task<Result<TirillaResponse>> ObtenerTirillaAsync(Guid boletoId, CancellationToken cancellationToken)
    {
        var boleto = await QueryBoletos().FirstOrDefaultAsync(b => b.BoletoId == boletoId, cancellationToken);
        if (boleto is null)
        {
            return Result<TirillaResponse>.Fail(BoletoMessages.BoletoNoEncontrado, 404);
        }

        return Result<TirillaResponse>.Ok(new TirillaResponse
        {
            CodigoImpreso = CodigoPublicoGenerator.FormatoImpreso(boleto.CodigoPublico),
            Fecha = boleto.Venta?.FechaVenta ?? boleto.FechaCreacion,
            Vendedor = boleto.Venta?.Usuario?.Alias ?? boleto.Venta?.Usuario?.NombreCompleto ?? string.Empty,
            Total = boleto.Venta?.Total ?? 0,
            TipoApuesta = boleto.Venta?.TipoApuesta ?? TipoApuesta.COMBINADO,
            VigenciaDias = boleto.VigenciaDias > 0 ? boleto.VigenciaDias : 30,
            Qr = await AsegurarQrCifradoAsync(boleto, cancellationToken),
            Juegos = boleto.Juegos.Select(MapJuego).ToList(),
            Leyenda = await ComponerLeyendaAsync(boleto.VigenciaDias > 0 ? boleto.VigenciaDias : 30, cancellationToken)
        }, SuccessMessages.OperacionExitosa);
    }

    private async Task<string> ComponerLeyendaAsync(int vigenciaDias, CancellationToken cancellationToken)
    {
        var item = await _db.Configuraciones.FirstOrDefaultAsync(c => c.Clave == ConfiguracionClaves.LeyendaTirilla, cancellationToken);
        return TirillaCuerpo.Leyenda(vigenciaDias, item?.Valor);
    }

    public async Task<Result<IReadOnlyList<BoletoListaResponse>>> FiltrarAsync(FiltroBoletosRequest request, CancellationToken cancellationToken)
    {
        var query = QueryBoletos();

        if (request.VendedorId.HasValue)
        {
            query = query.Where(b => b.Venta!.UsuarioId == request.VendedorId);
        }

        if (!string.IsNullOrWhiteSpace(request.CodigoPublico))
        {
            query = query.Where(b => b.CodigoPublico == request.CodigoPublico);
        }

        if (!string.IsNullOrWhiteSpace(request.Numero))
        {
            query = query.Where(b => b.Juegos.Any(j => j.Numero == request.Numero));
        }

        if (request.LoteriaId.HasValue)
        {
            query = query.Where(b => b.Juegos.Any(j => j.JuegoLoterias.Any(l => l.LoteriaId == request.LoteriaId)));
        }

        if (request.FechaInicial.HasValue)
        {
            query = query.Where(b => b.FechaCreacion >= request.FechaInicial);
        }

        if (request.FechaFinal.HasValue)
        {
            query = query.Where(b => b.FechaCreacion <= request.FechaFinal);
        }

        var boletos = await query.OrderByDescending(b => b.FechaCreacion).ToListAsync(cancellationToken);
        var lista = new List<BoletoListaResponse>();
        foreach (var boleto in boletos)
        {
            var estado = (await Evaluar(boleto, cancellationToken)).ResultadoVisual;
            if (!string.IsNullOrWhiteSpace(request.Estado) &&
                !string.Equals(estado, request.Estado, StringComparison.OrdinalIgnoreCase) &&
                !CoincideFiltroEstado(request.Estado, estado, boleto.EstadoBoleto))
            {
                continue;
            }

            lista.Add(new BoletoListaResponse
            {
                BoletoId = boleto.BoletoId,
                CodigoPublico = boleto.CodigoPublico,
                Vendedor = boleto.Venta?.Usuario?.Alias ?? boleto.Venta?.Usuario?.NombreCompleto ?? string.Empty,
                Fecha = boleto.Venta?.FechaVenta ?? boleto.FechaCreacion,
                Total = boleto.Venta?.Total ?? 0,
                Estado = estado
            });
        }

        return Result<IReadOnlyList<BoletoListaResponse>>.Ok(lista, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<ConsultaTicketResponse>> ConsultarPorCodigoAsync(string? ticketCode, CancellationToken cancellationToken)
    {
        var bruto = (ticketCode ?? string.Empty).Trim();
        if (bruto.Length == 0)
        {
            return Result<ConsultaTicketResponse>.Fail(PremioMessages.TicketRequerido);
        }

        if (bruto.Length > TicketCodeLimits.MaxInputLength)
        {
            return Result<ConsultaTicketResponse>.Fail(PremioMessages.TicketDemasiadoLargo);
        }

        var boleto = await BoletoPorCodigo.BuscarAsync(QueryBoletos(), _qr, bruto, cancellationToken);
        if (boleto is null)
        {
            return Result<ConsultaTicketResponse>.Fail(PremioMessages.TicketNoEncontrado, 404);
        }

        var evaluacion = await Evaluar(boleto, cancellationToken);
        var vista = TicketConsultaPresentacion.De(evaluacion.ResultadoVisual);
        var tirilla = await ObtenerTirillaAsync(boleto.BoletoId, cancellationToken);
        var tieneCaso = await _db.CasosGanadores.AnyAsync(c => c.BoletoId == boleto.BoletoId, cancellationToken);

        return Result<ConsultaTicketResponse>.Ok(new ConsultaTicketResponse
        {
            ResultadoVisual = evaluacion.ResultadoVisual,
            Mensaje = vista.Mensaje,
            Tono = vista.Tono,
            BoletoId = boleto.BoletoId,
            Tirilla = tirilla.Data,
            PuedeIniciarCaso = evaluacion.ResultadoVisual == BoletoMessages.BoletoGanador && !tieneCaso
        }, vista.Mensaje);
    }

    private async Task<string> AsegurarQrCifradoAsync(Boleto boleto, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(boleto.QrCifrado))
        {
            return boleto.QrCifrado;
        }

        var clave = _qr.GenerarClaveValidacion();
        boleto.ClaveValidacionHash = _qr.HashClaveValidacion(clave);
        var existente = await _db.ClavesValidacionBoleto.FirstOrDefaultAsync(c => c.BoletoId == boleto.BoletoId, cancellationToken);
        var identificador = existente?.IdentificadorClave ?? Guid.NewGuid();
        if (existente is null)
        {
            _db.ClavesValidacionBoleto.Add(new ClaveValidacionBoleto
            {
                ClaveId = Guid.NewGuid(),
                BoletoId = boleto.BoletoId,
                ClaveHash = boleto.ClaveValidacionHash,
                Version = 1,
                IdentificadorClave = identificador,
                FechaCreacion = _clock.UtcNow
            });
        }
        else
        {
            existente.ClaveHash = boleto.ClaveValidacionHash;
        }

        boleto.QrCifrado = _qr.Encrypt(new QrPayload(boleto.BoletoId, boleto.CodigoPublico, clave, 1, identificador));
        await _db.SaveChangesAsync(cancellationToken);
        return boleto.QrCifrado;
    }

    private IQueryable<Boleto> QueryBoletos() =>
        _db.Boletos
            .Include(b => b.Venta)!.ThenInclude(v => v!.Usuario)
            .Include(b => b.Juegos)
            .ThenInclude(j => j.JuegoLoterias)
            .ThenInclude(l => l.Loteria);

    private async Task<ValidacionBoletoResponse> Evaluar(Boleto boleto, CancellationToken cancellationToken)
    {
        var fechaJuego = DateOnly.FromDateTime((boleto.Venta?.FechaVenta ?? boleto.FechaCreacion).Date);
        var vigenciaHasta = (boleto.Venta?.FechaVenta ?? boleto.FechaCreacion).Date.AddDays(boleto.VigenciaDias);
        var juegos = boleto.Juegos.Select(MapJuego).ToList();

        var baseResponse = new ValidacionBoletoResponse
        {
            BoletoId = boleto.BoletoId,
            CodigoPublico = boleto.CodigoPublico,
            Vendedor = boleto.Venta?.Usuario?.Alias ?? boleto.Venta?.Usuario?.NombreCompleto,
            Fecha = boleto.Venta?.FechaVenta ?? boleto.FechaCreacion,
            Total = boleto.Venta?.Total,
            Juegos = juegos,
            Vigencia = vigenciaHasta.ToString("yyyy-MM-dd")
        };

        if (boleto.EstadoBoleto == EstadoBoleto.PagadoCobrado)
        {
            baseResponse.ResultadoVisual = BoletoMessages.BoletoPagado;
            baseResponse.Estado = BoletoMessages.BoletoPagado;
            return baseResponse;
        }

        if (boleto.EstadoBoleto == EstadoBoleto.PremioEntregado)
        {
            baseResponse.ResultadoVisual = BoletoMessages.BoletoPremioEntregado;
            baseResponse.Estado = BoletoMessages.BoletoPremioEntregado;
            return baseResponse;
        }

        if (boleto.EstadoBoleto == EstadoBoleto.PorJugar)
        {
            baseResponse.ResultadoVisual = BoletoMessages.BoletoPorJugar;
            baseResponse.Estado = BoletoMessages.BoletoPorJugar;
            return baseResponse;
        }

        if (boleto.EstadoBoleto == EstadoBoleto.Vencido)
        {
            baseResponse.ResultadoVisual = BoletoMessages.BoletoVencido;
            baseResponse.Estado = BoletoMessages.BoletoVencido;
            return baseResponse;
        }

        var loteriaIds = boleto.Juegos.SelectMany(j => j.JuegoLoterias.Select(l => l.LoteriaId)).Distinct().ToList();
        var resultados = await _db.NumerosGanadores
            .Where(n => n.FechaJuego == fechaJuego.ToDateTime(TimeOnly.MinValue) && loteriaIds.Contains(n.LoteriaId))
            .ToListAsync(cancellationToken);

        var gano = boleto.Juegos.Any(j =>
            j.JuegoLoterias.Any(l =>
                resultados.Any(r => r.LoteriaId == l.LoteriaId && r.Numero == j.Numero)));

        if (gano)
        {
            if (_clock.LocalNow.Date > vigenciaHasta)
            {
                baseResponse.ResultadoVisual = BoletoMessages.BoletoVencido;
                baseResponse.Estado = BoletoMessages.BoletoVencido;
                return baseResponse;
            }

            baseResponse.ResultadoVisual = BoletoMessages.BoletoGanador;
            baseResponse.Estado = BoletoMessages.BoletoGanador;
            return baseResponse;
        }

        if (resultados.Count == 0)
        {
            baseResponse.ResultadoVisual = BoletoMessages.BoletoJugado;
            baseResponse.Estado = BoletoMessages.BoletoJugado;
            return baseResponse;
        }

        baseResponse.ResultadoVisual = BoletoMessages.BoletoNoGanador;
        baseResponse.Estado = BoletoMessages.BoletoNoGanador;
        return baseResponse;
    }

    private static bool CoincideFiltroEstado(string filtro, string visual, EstadoBoleto estado) =>
        filtro.Equals("por jugar", StringComparison.OrdinalIgnoreCase) && estado == EstadoBoleto.PorJugar ||
        filtro.Equals("jugados", StringComparison.OrdinalIgnoreCase) && visual == BoletoMessages.BoletoJugado ||
        filtro.Equals("ganadores", StringComparison.OrdinalIgnoreCase) && visual == BoletoMessages.BoletoGanador ||
        filtro.Equals("no ganadores", StringComparison.OrdinalIgnoreCase) && visual == BoletoMessages.BoletoNoGanador ||
        filtro.Equals("vencidos", StringComparison.OrdinalIgnoreCase) && visual == BoletoMessages.BoletoVencido ||
        filtro.Equals("pagados", StringComparison.OrdinalIgnoreCase) && visual == BoletoMessages.BoletoPagado ||
        filtro.Equals("cobrados", StringComparison.OrdinalIgnoreCase) && visual == BoletoMessages.BoletoPagado;

    private static JuegoResponse MapJuego(Juego juego) => new()
    {
        JuegoId = juego.JuegoId,
        Numero = juego.Numero,
        Valor = juego.Valor,
        Total = TotalesApuesta.TotalJuego(juego.Valor, juego.JuegoLoterias.Count),
        Loterias = juego.JuegoLoterias.Select(x => x.Loteria?.Nombre ?? string.Empty).ToList()
    };
}
