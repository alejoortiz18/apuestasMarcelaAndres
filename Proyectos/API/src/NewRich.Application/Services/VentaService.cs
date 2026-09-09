using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Ventas;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class VentaService : IVentaService
{
    private readonly INewRichDbContext _db;
    private readonly IQrCryptoService _qr;
    private readonly IClock _clock;

    public VentaService(INewRichDbContext db, IQrCryptoService qr, IClock clock)
    {
        _db = db;
        _qr = qr;
        _clock = clock;
    }

    public async Task<Result<VentaResponse>> ConfirmarAsync(
        Guid vendedorId,
        Guid? dispositivoId,
        ConfirmarVentaRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result<VentaResponse>.Fail(ValidationMessages.IdempotencyKeyRequerida);
        }

        var existente = await _db.Ventas
            .Include(v => v.Usuario)
            .Include(v => v.Boletos)
            .ThenInclude(b => b.Juegos)
            .ThenInclude(j => j.JuegoLoterias)
            .ThenInclude(jl => jl.Loteria)
            .FirstOrDefaultAsync(v => v.IdempotencyKey == idempotencyKey, cancellationToken);

        if (existente is not null)
        {
            var boletoExistente = existente.Boletos.First();
            return Result<VentaResponse>.Ok(await MapVenta(existente, boletoExistente, cancellationToken), VentaMessages.VentaDuplicadaIdempotente);
        }

        if (request.Juegos is null || request.Juegos.Count == 0)
        {
            return Result<VentaResponse>.Fail(VentaMessages.VentaSinLineas);
        }

        var maximo = await _db.ConfiguracionesTipoApuesta
            .FirstOrDefaultAsync(x => x.TipoApuesta == request.TipoApuesta.ToString(), cancellationToken);
        if (maximo is not null && request.Juegos.Count > maximo.Maximo)
        {
            return Result<VentaResponse>.Fail(VentaMessages.MaximoLineasExcedido);
        }

        if (await EstaFueraDeHorario(cancellationToken))
        {
            return Result<VentaResponse>.Fail(VentaMessages.VentaFueraDeHorario, 403);
        }

        try
        {
            VentaResponse? respuesta = null;
            await _db.ExecuteInTransactionAsync(async ct =>
        {
            var vendedor = await _db.Usuarios.FirstAsync(u => u.UsuarioId == vendedorId, ct);
            var vigencia = await ObtenerVigencia(ct);
            var alertaRepeticion = await ObtenerEntero("AlertaRepeticionNumero", 10, ct);
            var alertaValor = await ObtenerEntero("AlertaValorMinimo", 10000, ct);

            var venta = new Venta
            {
                VentaId = Guid.NewGuid(),
                UsuarioId = vendedorId,
                DispositivoId = dispositivoId,
                FechaVenta = _clock.UtcNow,
                TipoApuesta = request.TipoApuesta,
                EstadoSincronizacion = "Online",
                IdempotencyKey = idempotencyKey,
                FechaSincronizacion = _clock.UtcNow
            };

            var boleto = new Boleto
            {
                BoletoId = Guid.NewGuid(),
                VentaId = venta.VentaId,
                EstadoBoleto = EstadoBoleto.Jugado,
                FechaCreacion = _clock.UtcNow,
                VigenciaDias = vigencia
            };

            decimal total = 0;
            foreach (var linea in request.Juegos)
            {
                if (string.IsNullOrWhiteSpace(linea.Numero) || linea.Numero.Length != 4 || !linea.Numero.All(char.IsDigit))
                {
                    throw new InvalidOperationException(ValidationMessages.NumeroApuestaFormato);
                }

                if (linea.Valor <= 0)
                {
                    throw new InvalidOperationException(ValidationMessages.ValorApuestaMayorCero);
                }

                if (linea.LoteriaIds is null || linea.LoteriaIds.Count == 0)
                {
                    throw new InvalidOperationException(ValidationMessages.LoteriasRequeridas);
                }

                var loterias = await _db.Loterias.Where(l => linea.LoteriaIds.Contains(l.LoteriaId)).ToListAsync(ct);
                if (loterias.Count != linea.LoteriaIds.Distinct().Count())
                {
                    throw new InvalidOperationException(VentaMessages.LoteriaNoEncontrada);
                }

                if (loterias.Any(l => l.Estado != EstadoGeneral.Activo))
                {
                    throw new InvalidOperationException(VentaMessages.LoteriaInactiva);
                }

                var juego = new Juego
                {
                    JuegoId = Guid.NewGuid(),
                    BoletoId = boleto.BoletoId,
                    Numero = linea.Numero,
                    Valor = linea.Valor,
                    TipoJuego = request.TipoApuesta == TipoApuesta.COMBINADO ? TipoJuego.COMBINADA : TipoJuego.INDIVIDUAL
                };

                foreach (var loteria in loterias)
                {
                    juego.JuegoLoterias.Add(new JuegoLoteria { JuegoId = juego.JuegoId, LoteriaId = loteria.LoteriaId, Loteria = loteria });
                }

                total += TotalesApuesta.TotalJuego(linea.Valor, loterias.Count);
                boleto.Juegos.Add(juego);

                var admins = await _db.Usuarios.Where(u => u.Rol == RolUsuario.Administrador && u.Estado == EstadoUsuario.Activo).Select(u => u.UsuarioId).ToListAsync(ct);
                var repeticiones = await _db.Juegos.CountAsync(j => j.Numero == linea.Numero && j.Boleto!.FechaCreacion.Date == _clock.UtcNow.Date, ct);
                if (repeticiones + 1 >= alertaRepeticion)
                {
                    foreach (var adminId in admins)
                    {
                        _db.Notificaciones.Add(new Notificacion
                        {
                            NotificacionId = Guid.NewGuid(),
                            UsuarioId = adminId,
                            Tipo = "RepeticionNumero",
                            Mensaje = string.Format(VentaMessages.AlertaRepeticionNumero, linea.Numero),
                            FechaCreacion = _clock.UtcNow
                        });
                    }
                }

                if (linea.Valor >= alertaValor)
                {
                    foreach (var adminId in admins)
                    {
                        _db.Notificaciones.Add(new Notificacion
                        {
                            NotificacionId = Guid.NewGuid(),
                            UsuarioId = adminId,
                            Tipo = "ValorAlto",
                            Mensaje = string.Format(VentaMessages.AlertaValorAlto, linea.Valor.ToString("N0")),
                            FechaCreacion = _clock.UtcNow
                        });
                    }
                }
            }

            venta.Total = total;
            boleto.CodigoPublico = await GenerarCodigoPublico(ct);
            var clave = _qr.GenerarClaveValidacion();
            boleto.ClaveValidacionHash = _qr.HashClaveValidacion(clave);

            var claveEntity = new ClaveValidacionBoleto
            {
                ClaveId = Guid.NewGuid(),
                BoletoId = boleto.BoletoId,
                ClaveHash = boleto.ClaveValidacionHash,
                Version = 1,
                IdentificadorClave = Guid.NewGuid(),
                FechaCreacion = _clock.UtcNow
            };

            var qr = _qr.Encrypt(new QrPayload(boleto.BoletoId, boleto.CodigoPublico, clave, 1, claveEntity.IdentificadorClave));

            venta.Boletos.Add(boleto);
            _db.Ventas.Add(venta);
            _db.ClavesValidacionBoleto.Add(claveEntity);
            await _db.SaveChangesAsync(ct);

            respuesta = new VentaResponse
            {
                VentaId = venta.VentaId,
                BoletoId = boleto.BoletoId,
                CodigoPublico = boleto.CodigoPublico,
                CodigoImpreso = CodigoPublicoGenerator.FormatoImpreso(boleto.CodigoPublico),
                Qr = qr,
                VendedorId = vendedor.UsuarioId,
                Vendedor = vendedor.Alias ?? vendedor.NombreCompleto,
                FechaVenta = venta.FechaVenta,
                Total = venta.Total,
                EstadoBoleto = BoletoMessages.BoletoJugado,
                Juegos = boleto.Juegos.Select(MapJuego).ToList()
            };
        }, cancellationToken);

            return Result<VentaResponse>.Created(respuesta!, SuccessMessages.VentaConfirmada);
        }
        catch (InvalidOperationException ex)
        {
            return Result<VentaResponse>.Fail(ex.Message);
        }
    }

    public async Task<Result<IReadOnlyList<VentaResponse>>> ConsultarAsync(
        ConsultaVentasRequest request,
        Guid solicitanteId,
        bool soloPropias,
        CancellationToken cancellationToken)
    {
        var query = _db.Ventas
            .Include(v => v.Usuario)
            .Include(v => v.Boletos)
            .ThenInclude(b => b.Juegos)
            .ThenInclude(j => j.JuegoLoterias)
            .ThenInclude(jl => jl.Loteria)
            .AsQueryable();

        if (soloPropias)
        {
            query = query.Where(v => v.UsuarioId == solicitanteId);
            if (request.FechaInicial.HasValue && request.FechaFinal.HasValue)
            {
                var min = DateTime.UtcNow.Date.AddDays(-10);
                if (request.FechaInicial.Value.Date < min)
                {
                    request.FechaInicial = min;
                }
            }
        }
        else if (request.VendedorId.HasValue)
        {
            query = query.Where(v => v.UsuarioId == request.VendedorId);
        }

        if (request.FechaInicial.HasValue)
        {
            query = query.Where(v => v.FechaVenta >= request.FechaInicial);
        }

        if (request.FechaFinal.HasValue)
        {
            query = query.Where(v => v.FechaVenta <= request.FechaFinal);
        }

        if (!string.IsNullOrWhiteSpace(request.Numero))
        {
            query = query.Where(v => v.Boletos.Any(b => b.Juegos.Any(j => j.Numero == request.Numero)));
        }

        if (request.LoteriaId.HasValue)
        {
            query = query.Where(v => v.Boletos.Any(b => b.Juegos.Any(j => j.JuegoLoterias.Any(l => l.LoteriaId == request.LoteriaId))));
        }

        var ventas = await query.OrderByDescending(v => v.FechaVenta).ToListAsync(cancellationToken);
        var result = new List<VentaResponse>();
        foreach (var venta in ventas)
        {
            var boleto = venta.Boletos.First();
            result.Add(await MapVenta(venta, boleto, cancellationToken));
        }

        return Result<IReadOnlyList<VentaResponse>>.Ok(result, SuccessMessages.OperacionExitosa);
    }

    private async Task<VentaResponse> MapVenta(Venta venta, Boleto boleto, CancellationToken cancellationToken)
    {
        var clave = await _db.ClavesValidacionBoleto.FirstOrDefaultAsync(c => c.BoletoId == boleto.BoletoId, cancellationToken);
        return new VentaResponse
        {
            VentaId = venta.VentaId,
            BoletoId = boleto.BoletoId,
            CodigoPublico = boleto.CodigoPublico,
            CodigoImpreso = CodigoPublicoGenerator.FormatoImpreso(boleto.CodigoPublico),
            Qr = string.Empty,
            VendedorId = venta.UsuarioId,
            Vendedor = venta.Usuario?.Alias ?? venta.Usuario?.NombreCompleto ?? string.Empty,
            FechaVenta = venta.FechaVenta,
            Total = venta.Total,
            EstadoBoleto = boleto.EstadoBoleto.ToString(),
            Juegos = boleto.Juegos.Select(MapJuego).ToList()
        };
    }

    private static JuegoResponse MapJuego(Juego juego) => new()
    {
        JuegoId = juego.JuegoId,
        Numero = juego.Numero,
        Valor = juego.Valor,
        Total = TotalesApuesta.TotalJuego(juego.Valor, juego.JuegoLoterias.Count),
        Loterias = juego.JuegoLoterias.Select(x => x.Loteria?.Nombre ?? string.Empty).ToList()
    };

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

    private async Task<bool> EstaFueraDeHorario(CancellationToken cancellationToken)
    {
        var hora = await _db.Configuraciones.FirstOrDefaultAsync(c => c.Clave == "HoraCierre", cancellationToken);
        if (hora is null || !TimeSpan.TryParse(hora.Valor, out var cierre))
        {
            return false;
        }

        return _clock.LocalNow.TimeOfDay > cierre;
    }

    private async Task<int> ObtenerVigencia(CancellationToken cancellationToken)
    {
        var item = await _db.Configuraciones.FirstOrDefaultAsync(c => c.Clave == "VigenciaPremiosDias", cancellationToken);
        return item is not null && int.TryParse(item.Valor, out var dias) ? dias : 30;
    }

    private async Task<int> ObtenerEntero(string clave, int defecto, CancellationToken cancellationToken)
    {
        var item = await _db.Configuraciones.FirstOrDefaultAsync(c => c.Clave == clave, cancellationToken);
        return item is not null && int.TryParse(item.Valor, out var valor) ? valor : defecto;
    }
}
