using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Offline;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
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

    public async Task<Result<SincronizarVentasOfflineResponse>> SincronizarVentasAsync(
        Guid vendedorId,
        SincronizarVentasOfflineRequest request,
        CancellationToken cancellationToken)
    {
        var sincronizados = new List<string>();
        foreach (var qr in request.QrJson ?? [])
        {
            var resultado = await IngestarAsync(qr, vendedorId, null, cancellationToken);
            if (resultado.IsSuccess && resultado.Data is not null)
            {
                sincronizados.Add(resultado.Data.Consecutivo);
            }
        }

        return Result<SincronizarVentasOfflineResponse>.Ok(
            new SincronizarVentasOfflineResponse { Sincronizados = sincronizados },
            SuccessMessages.VentasOfflineSincronizadas);
    }

    public Task<Result<CodigoOfflineResponse>> RegistrarQrAsync(
        Guid administradorId,
        RegistrarQrOfflineRequest request,
        CancellationToken cancellationToken) =>
        IngestarAsync(request.Qr, null, administradorId, cancellationToken);

    private async Task<Result<CodigoOfflineResponse>> IngestarAsync(
        string? qrJson,
        Guid? vendedorId,
        Guid? administradorId,
        CancellationToken cancellationToken)
    {
        if (!SobreQrOfflineCodec.TryLeer(qrJson, out var sobre))
        {
            return Result<CodigoOfflineResponse>.Fail(UsuarioMessages.QrInvalidoOAlterado);
        }

        QrPayload? payload = null;
        CodigoPreventaOffline? codigo = null;
        if (sobre.EsLlaveCorta)
        {
            codigo = await _db.CodigosPreventaOffline
                .Include(c => c.Usuario)
                .Include(c => c.Dispositivo)
                .FirstOrDefaultAsync(
                    c => c.ConsecutivoUnico == sobre.Consecutivo,
                    cancellationToken);
            var secreto = codigo is null ? string.Empty : Encoding.UTF8.GetString(codigo.PayloadCifrado);
            if (codigo is null
                || !SobreQrOfflineCodec.SelloCoincide(secreto, codigo.ConsecutivoUnico, sobre.Sello))
            {
                return Result<CodigoOfflineResponse>.Fail(UsuarioMessages.QrInvalidoOAlterado);
            }

            payload = _qr.Decrypt(secreto);
        }
        else
        {
            payload = _qr.Decrypt(sobre.Codigo);
            if (payload is not null)
            {
                codigo = await _db.CodigosPreventaOffline
                    .Include(c => c.Usuario)
                    .Include(c => c.Dispositivo)
                    .FirstOrDefaultAsync(c => c.CodigoId == payload.BoletoId, cancellationToken);
            }

            if (codigo is null)
            {
                var todos = await _db.CodigosPreventaOffline
                    .Include(c => c.Usuario)
                    .Include(c => c.Dispositivo)
                    .ToListAsync(cancellationToken);
                codigo = todos.FirstOrDefault(c =>
                    Encoding.UTF8.GetString(c.PayloadCifrado) == sobre.Codigo
                    && string.Equals(c.ConsecutivoUnico, sobre.Consecutivo, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (codigo is null
            || !string.Equals(codigo.ConsecutivoUnico, sobre.Consecutivo, StringComparison.OrdinalIgnoreCase)
            || payload is null)
        {
            return Result<CodigoOfflineResponse>.Fail(UsuarioMessages.QrInvalidoOAlterado);
        }

        if (vendedorId is Guid vendedor && codigo.UsuarioId != vendedor)
        {
            return Result<CodigoOfflineResponse>.Fail(UsuarioMessages.QrInvalidoOAlterado);
        }

        var yaHabiaVenta = codigo.VentaId.HasValue
            || await _db.Boletos.AnyAsync(b => b.BoletoId == codigo.CodigoId, cancellationToken);

        if (!yaHabiaVenta)
        {
            if (sobre.EsLlaveCorta)
            {
                return Result<CodigoOfflineResponse>.Fail(UsuarioMessages.QrPendienteDeSincronizar);
            }

            var creada = await CrearVentaOficialAsync(codigo, payload, sobre, cancellationToken);
            if (!creada.IsSuccess)
            {
                return Result<CodigoOfflineResponse>.Fail(creada.Message, creada.StatusCode);
            }
        }

        if (administradorId is Guid admin)
        {
            if (codigo.EstadoDelCodigo != EstadoCodigoOffline.Registrado)
            {
                codigo.EstadoDelCodigo = EstadoCodigoOffline.Registrado;
                codigo.FechaRegistro = _clock.UtcNow;
                codigo.AdminQueRegistro = admin;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return Result<CodigoOfflineResponse>.Ok(
                Map(codigo),
                yaHabiaVenta ? UsuarioMessages.QrYaRegistrado : SuccessMessages.CodigoOfflineRegistrado);
        }

        if (codigo.EstadoDelCodigo != EstadoCodigoOffline.Registrado)
        {
            codigo.EstadoDelCodigo = EstadoCodigoOffline.Utilizado;
            codigo.FechaVentaOffline ??= sobre.Jugada.Fecha == default ? _clock.UtcNow : sobre.Jugada.Fecha.ToUniversalTime();
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Result<CodigoOfflineResponse>.Ok(Map(codigo), SuccessMessages.VentasOfflineSincronizadas);
    }

    private async Task<Result> CrearVentaOficialAsync(
        CodigoPreventaOffline codigo,
        QrPayload payload,
        SobreQrOffline sobre,
        CancellationToken cancellationToken)
    {
        if (sobre.Jugada.Lineas.Count == 0)
        {
            return Result.Fail(UsuarioMessages.QrInvalidoOAlterado);
        }

        if (!Enum.TryParse<TipoApuesta>(sobre.Jugada.Tipo, true, out var tipo))
        {
            tipo = TipoApuesta.INDIVIDUAL;
        }

        var vigencia = 30;
        var cfg = await _db.Configuraciones.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Clave == "VigenciaPremiosDias", cancellationToken);
        if (cfg is not null && int.TryParse(cfg.Valor, out var dias) && dias > 0)
        {
            vigencia = dias;
        }

        decimal total = 0;
        var juegos = new List<Juego>();
        foreach (var linea in sobre.Jugada.Lineas)
        {
            if (!NumeroApuesta.EsValido(linea.Numero))
            {
                return Result.Fail(UsuarioMessages.QrInvalidoOAlterado);
            }

            var ids = linea.LoteriaIds.Distinct().ToArray();
            if (ids.Length == 0)
            {
                return Result.Fail(UsuarioMessages.QrInvalidoOAlterado);
            }

            var loterias = await _db.Loterias.Where(l => ids.Contains(l.LoteriaId)).ToListAsync(cancellationToken);
            if (loterias.Count != ids.Length)
            {
                return Result.Fail(UsuarioMessages.QrInvalidoOAlterado);
            }

            var juego = new Juego
            {
                JuegoId = Guid.NewGuid(),
                BoletoId = codigo.CodigoId,
                Numero = linea.Numero.Trim(),
                Valor = linea.Valor,
                TipoJuego = tipo == TipoApuesta.COMBINADO ? TipoJuego.COMBINADA : TipoJuego.INDIVIDUAL
            };
            foreach (var loteria in loterias)
            {
                juego.JuegoLoterias.Add(new JuegoLoteria { JuegoId = juego.JuegoId, LoteriaId = loteria.LoteriaId });
            }

            total += TotalesApuesta.TotalJuego(linea.Valor, loterias.Count);
            juegos.Add(juego);
        }

        var ventaId = Guid.NewGuid();
        var fecha = sobre.Jugada.Fecha == default ? _clock.UtcNow : sobre.Jugada.Fecha.ToUniversalTime();
        var venta = new Venta
        {
            VentaId = ventaId,
            UsuarioId = codigo.UsuarioId,
            DispositivoId = codigo.DispositivoId,
            FechaVenta = fecha,
            Total = total,
            TipoApuesta = tipo,
            EstadoSincronizacion = "Sincronizada",
            IdempotencyKey = $"offline:{codigo.ConsecutivoUnico}",
            FechaSincronizacion = _clock.UtcNow
        };

        var boleto = new Boleto
        {
            BoletoId = codigo.CodigoId,
            VentaId = ventaId,
            CodigoPublico = payload.CodigoPublico,
            ClaveValidacionHash = _qr.HashClaveValidacion(payload.ClaveValidacion),
            QrCifrado = SobreQrOfflineCodec.Armar(sobre.Codigo, sobre.Consecutivo, sobre.Jugada),
            EstadoBoleto = EstadoBoleto.Jugado,
            FechaCreacion = fecha,
            VigenciaDias = vigencia
        };
        foreach (var juego in juegos)
        {
            boleto.Juegos.Add(juego);
        }

        var clave = new ClaveValidacionBoleto
        {
            ClaveId = payload.IdentificadorClave == Guid.Empty ? Guid.NewGuid() : payload.IdentificadorClave,
            BoletoId = boleto.BoletoId,
            ClaveHash = boleto.ClaveValidacionHash,
            Version = payload.Version <= 0 ? 1 : payload.Version,
            IdentificadorClave = payload.IdentificadorClave == Guid.Empty ? Guid.NewGuid() : payload.IdentificadorClave,
            FechaCreacion = _clock.UtcNow
        };

        _db.Ventas.Add(venta);
        _db.Boletos.Add(boleto);
        _db.ClavesValidacionBoleto.Add(clave);
        codigo.VentaId = ventaId;
        codigo.FechaVentaOffline ??= fecha;
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.VentasOfflineSincronizadas);
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
