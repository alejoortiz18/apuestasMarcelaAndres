using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Offline;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class OfflineService : IOfflineService
{
    private readonly INewRichDbContext _db;
    private readonly IQrCryptoService _qr;
    private readonly IClock _clock;
    private readonly ICodigosOfflineTiempoReal _vivo;

    public OfflineService(INewRichDbContext db, IQrCryptoService qr, IClock clock, ICodigosOfflineTiempoReal vivo)
    {
        _db = db;
        _qr = qr;
        _clock = clock;
        _vivo = vivo;
    }

    public async Task<Result<OfflineListadoResponse>> ListarAsync(CancellationToken cancellationToken)
    {
        var items = await _db.CodigosPreventaOffline
            .Include(c => c.Usuario)
            .Include(c => c.Dispositivo)
            .ToListAsync(cancellationToken);

        var mapped = items
            .OrderByDescending(c => NumeroConsecutivo(c.ConsecutivoUnico))
            .Select(Map)
            .ToList();

        return Result<OfflineListadoResponse>.Ok(
            new OfflineListadoResponse
            {
                Resumen = ResumenDe(items),
                Codigos = mapped
            },
            SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<CodigoOfflineResponse>> ObtenerAsync(Guid codigoId, CancellationToken cancellationToken)
    {
        var codigo = await _db.CodigosPreventaOffline
            .Include(c => c.Usuario)
            .Include(c => c.Dispositivo)
            .FirstOrDefaultAsync(c => c.CodigoId == codigoId, cancellationToken);
        if (codigo is null)
        {
            return Result<CodigoOfflineResponse>.Fail(UsuarioMessages.CodigoOfflineNoEncontrado, 404);
        }

        return Result<CodigoOfflineResponse>.Ok(Map(codigo), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<IReadOnlyList<CodigoOfflineResponse>>> GenerarAsync(
        GenerarCodigosOfflineRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Cantidad is < 1 or > 5000)
        {
            return Result<IReadOnlyList<CodigoOfflineResponse>>.Fail(ValidationMessages.CantidadCodigosOfflineRango);
        }

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == request.UsuarioId, cancellationToken);
        if (usuario is null)
        {
            return Result<IReadOnlyList<CodigoOfflineResponse>>.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        if (usuario.Rol != RolUsuario.Vendedor)
        {
            return Result<IReadOnlyList<CodigoOfflineResponse>>.Fail(AuthMessages.SoloVendedor);
        }

        var pda = await _db.Dispositivos.FirstOrDefaultAsync(d => d.DispositivoId == request.DispositivoId, cancellationToken);
        if (pda is null)
        {
            return Result<IReadOnlyList<CodigoOfflineResponse>>.Fail(UsuarioMessages.DispositivoNoEncontrado, 404);
        }

        if (pda.Estado != EstadoGeneral.Activo)
        {
            return Result<IReadOnlyList<CodigoOfflineResponse>>.Fail(AuthMessages.DispositivoInactivo);
        }

        var asociado = await _db.DispositivosUsuarios.AnyAsync(
            x => x.DispositivoId == request.DispositivoId && x.UsuarioId == request.UsuarioId && x.Activo,
            cancellationToken);
        if (!asociado)
        {
            return Result<IReadOnlyList<CodigoOfflineResponse>>.Fail(AuthMessages.DispositivoNoAsociado);
        }

        var ocupados = await _db.CodigosPreventaOffline.CountAsync(
            c => c.DispositivoId == request.DispositivoId
                && (c.EstadoDelCodigo == EstadoCodigoOffline.Generado
                    || c.EstadoDelCodigo == EstadoCodigoOffline.Descargado),
            cancellationToken);
        if (ocupados + request.Cantidad > pda.CapacidadCodigosOffline)
        {
            return Result<IReadOnlyList<CodigoOfflineResponse>>.Fail(UsuarioMessages.CapacidadCodigosOfflineInsuficiente);
        }

        var existentes = await _db.CodigosPreventaOffline
            .Select(c => c.ConsecutivoUnico)
            .ToListAsync(cancellationToken);
        var siguiente = existentes.Select(NumeroConsecutivo).DefaultIfEmpty(0).Max() + 1;
        var creados = new List<CodigoPreventaOffline>();
        for (var i = 0; i < request.Cantidad; i++)
        {
            var boletoId = Guid.NewGuid();
            var payload = _qr.Encrypt(new QrPayload(
                boletoId,
                Random.Shared.Next(0, 10_000_000).ToString("0000000", CultureInfo.InvariantCulture),
                _qr.GenerarClaveValidacion(),
                1,
                Guid.Empty));
            var codigo = new CodigoPreventaOffline
            {
                CodigoId = boletoId,
                ConsecutivoUnico = $"OFF-{siguiente + i:000000}",
                UsuarioId = usuario.UsuarioId,
                DispositivoId = pda.DispositivoId,
                PayloadCifrado = Encoding.UTF8.GetBytes(payload),
                EstadoDelCodigo = EstadoCodigoOffline.Generado,
                FechaCreacion = _clock.UtcNow
            };
            creados.Add(codigo);
            _db.CodigosPreventaOffline.Add(codigo);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _vivo.AvisarAsignadosAsync(
            usuario.UsuarioId,
            new CodigosOfflineAsignadosAviso
            {
                DispositivoId = pda.DispositivoId,
                Cantidad = creados.Count
            },
            cancellationToken);
        return Result<IReadOnlyList<CodigoOfflineResponse>>.Created(
            creados.Select(c => Map(c, usuario.NombreCompleto, pda.CodigoDispositivo)).ToList(),
            SuccessMessages.CodigosOfflineGenerados);
    }

    private static OfflineResumenResponse ResumenDe(IReadOnlyList<CodigoPreventaOffline> items) => new()
    {
        Generados = items.Count(c => c.EstadoDelCodigo == EstadoCodigoOffline.Generado),
        Descargados = items.Count(c => c.EstadoDelCodigo == EstadoCodigoOffline.Descargado),
        Utilizados = items.Count(c => c.EstadoDelCodigo == EstadoCodigoOffline.Utilizado),
        Registrados = items.Count(c => c.EstadoDelCodigo == EstadoCodigoOffline.Registrado),
        PdasConDescarga = items
            .Where(c => c.EstadoDelCodigo == EstadoCodigoOffline.Descargado)
            .Select(c => c.DispositivoId)
            .Distinct()
            .Count()
    };

    private static CodigoOfflineResponse Map(CodigoPreventaOffline codigo) =>
        Map(codigo, codigo.Usuario?.NombreCompleto ?? string.Empty, codigo.Dispositivo?.CodigoDispositivo ?? string.Empty);

    private static CodigoOfflineResponse Map(CodigoPreventaOffline codigo, string usuario, string pda) => new()
    {
        CodigoId = codigo.CodigoId,
        Consecutivo = codigo.ConsecutivoUnico,
        UsuarioId = codigo.UsuarioId,
        Usuario = usuario,
        DispositivoId = codigo.DispositivoId,
        Pda = pda,
        FechaCreacion = codigo.FechaCreacion,
        FechaDescarga = codigo.FechaDescarga,
        FechaVenta = codigo.FechaVentaOffline,
        FechaRegistro = codigo.FechaRegistro,
        Estado = codigo.EstadoDelCodigo.ToString()
    };

    private static int NumeroConsecutivo(string valor)
    {
        if (valor.StartsWith("OFF-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(valor[4..], CultureInfo.InvariantCulture, out var numero))
        {
            return numero;
        }

        return 0;
    }
}
