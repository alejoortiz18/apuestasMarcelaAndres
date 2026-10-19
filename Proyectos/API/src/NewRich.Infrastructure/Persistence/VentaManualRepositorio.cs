using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;

namespace NewRich.Infrastructure.Persistence;

public sealed class VentaManualRepositorio : IVentaManualRepositorio
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly NewRichDbContext _db;

    public VentaManualRepositorio(NewRichDbContext db) => _db = db;

    public Task<VentaManual?> BuscarPorIdempotenciaAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        Consulta().FirstOrDefaultAsync(v => v.IdempotencyKey == idempotencyKey, cancellationToken);

    public Task<bool> ExisteCodigoAsync(string codigoPublico, CancellationToken cancellationToken) =>
        _db.BoletosManual.AnyAsync(b => b.CodigoPublico == codigoPublico, cancellationToken);

    public async Task GuardarAsync(VentaManual venta, CancellationToken cancellationToken)
    {
        if (!_db.Database.IsRelational())
        {
            _db.VentasManual.Add(venta);
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var boleto = venta.Boletos.First();
        var juegos = JsonSerializer.Serialize(boleto.Juegos.Select(j => new
        {
            juegoId = j.JuegoId,
            numero = j.Numero,
            valor = j.Valor,
            tipoJuego = j.TipoJuego.ToString(),
            loterias = j.Loterias.Select(l => l.LoteriaId).ToArray()
        }), Json);

        await _db.Database.ExecuteSqlInterpolatedAsync($@"
EXEC manual.ConfirmarVenta
    @VentaId={venta.VentaId},
    @UsuarioId={venta.UsuarioId},
    @FechaVenta={venta.FechaVenta},
    @Total={venta.Total},
    @TipoApuesta={venta.TipoApuesta.ToString()},
    @IdempotencyKey={venta.IdempotencyKey},
    @BoletoId={boleto.BoletoId},
    @CodigoPublico={boleto.CodigoPublico},
    @VigenciaDias={boleto.VigenciaDias},
    @FechaCreacion={boleto.FechaCreacion},
    @Juegos={juegos}", cancellationToken);
    }

    public async Task<IReadOnlyList<VentaManual>> ConsultarAsync(Guid usuarioId, DateTime desde, DateTime hasta, CancellationToken cancellationToken)
    {
        if (!_db.Database.IsRelational())
        {
            return await Consulta()
                .Where(v => v.UsuarioId == usuarioId && v.FechaVenta >= desde && v.FechaVenta <= hasta)
                .OrderByDescending(v => v.FechaVenta)
                .ToListAsync(cancellationToken);
        }

        var filas = await _db.Database.SqlQuery<FilaVentaManual>(
            $"EXEC manual.ConsultarVentas @UsuarioId={usuarioId}, @Desde={desde}, @Hasta={hasta}")
            .ToListAsync(cancellationToken);
        return Agrupar(filas);
    }

    public async Task<VentaManual?> BuscarPorCodigoAsync(Guid usuarioId, string codigoPublico, CancellationToken cancellationToken)
    {
        if (!_db.Database.IsRelational())
        {
            return await Consulta().FirstOrDefaultAsync(
                v => v.UsuarioId == usuarioId && v.Boletos.Any(b => b.CodigoPublico == codigoPublico),
                cancellationToken);
        }

        var filas = await _db.Database.SqlQuery<FilaVentaManual>(
            $"EXEC manual.ConsultarPorCodigo @UsuarioId={usuarioId}, @Codigo={codigoPublico}")
            .ToListAsync(cancellationToken);
        return Agrupar(filas).FirstOrDefault();
    }

    private IQueryable<VentaManual> Consulta() =>
        _db.VentasManual
            .Include(v => v.Boletos)
            .ThenInclude(b => b.Juegos)
            .ThenInclude(j => j.Loterias);

    private static IReadOnlyList<VentaManual> Agrupar(IReadOnlyList<FilaVentaManual> filas) =>
        filas.GroupBy(f => f.VentaId).Select(grupo =>
        {
            var cabecera = grupo.First();
            var venta = new VentaManual
            {
                VentaId = cabecera.VentaId,
                UsuarioId = cabecera.UsuarioId,
                FechaVenta = cabecera.FechaVenta,
                Total = cabecera.Total,
                TipoApuesta = Enum.Parse<TipoApuesta>(cabecera.TipoApuesta.Trim()),
                IdempotencyKey = cabecera.IdempotencyKey
            };
            var boleto = new BoletoManual
            {
                BoletoId = cabecera.BoletoId,
                VentaId = cabecera.VentaId,
                CodigoPublico = cabecera.CodigoPublico.Trim(),
                VigenciaDias = cabecera.VigenciaDias,
                EstadoBoleto = "Jugado"
            };
            foreach (var juegos in grupo.GroupBy(f => f.JuegoId))
            {
                var linea = juegos.First();
                var juego = new JuegoManual
                {
                    JuegoId = linea.JuegoId,
                    BoletoId = linea.BoletoId,
                    Numero = linea.Numero.Trim(),
                    Valor = linea.Valor,
                    TipoJuego = Enum.Parse<TipoJuego>(linea.TipoJuego.Trim())
                };
                foreach (var loteria in juegos)
                {
                    juego.Loterias.Add(new JuegoLoteriaManual
                    {
                        JuegoId = linea.JuegoId,
                        LoteriaId = loteria.LoteriaId,
                        NombreLoteria = loteria.LoteriaNombre
                    });
                }

                boleto.Juegos.Add(juego);
            }

            venta.Boletos.Add(boleto);
            return venta;
        }).ToList();

    private sealed class FilaVentaManual
    {
        public Guid VentaId { get; set; }
        public Guid UsuarioId { get; set; }
        public DateTime FechaVenta { get; set; }
        public decimal Total { get; set; }
        public string TipoApuesta { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
        public Guid BoletoId { get; set; }
        public string CodigoPublico { get; set; } = string.Empty;
        public int VigenciaDias { get; set; }
        public Guid JuegoId { get; set; }
        public string Numero { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string TipoJuego { get; set; } = string.Empty;
        public Guid LoteriaId { get; set; }
        public string LoteriaNombre { get; set; } = string.Empty;
    }
}
