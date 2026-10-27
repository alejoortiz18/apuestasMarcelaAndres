using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.ApkStore;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IApkStoreService
{
    Task<Result<LlaveFirmaResponse>> CrearLlaveAsync(Guid usuarioId, string password, string? ip, CancellationToken cancellationToken);

    Task<Result<LlaveFirmaDescargaResponse>> DescargarLlaveAsync(Guid usuarioId, string password, string? ip, CancellationToken cancellationToken);

    Task<Result<LlaveFirmaResponse>> ObtenerHuellaAsync(CancellationToken cancellationToken);

    Task<Result<VersionApkResponse>> PublicarAsync(
        Guid usuarioId,
        Stream contenido,
        long tamano,
        string nombreOriginal,
        string nombreVersion,
        int numeroCompilacion,
        string? ip,
        CancellationToken cancellationToken);

    Task<Result<VersionApkResponse>> ObtenerVigenteAsync(CancellationToken cancellationToken);

    Task<Result<ApkDescargaResponse>> AbrirVigenteAsync(CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<VersionApkResponse>>> ListarVersionesAsync(CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<AuditoriaApkResponse>>> ListarAuditoriaAsync(CancellationToken cancellationToken);
}

public sealed class ApkStoreService : IApkStoreService
{
    private readonly INewRichDbContext _db;
    private readonly IApkAlmacen _almacen;
    private readonly IGeneradorLlaveFirma _generador;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;
    private readonly IVersionesTiempoReal _vivo;

    public ApkStoreService(
        INewRichDbContext db,
        IApkAlmacen almacen,
        IGeneradorLlaveFirma generador,
        IPasswordHasher hasher,
        IClock clock,
        IVersionesTiempoReal vivo)
    {
        _db = db;
        _almacen = almacen;
        _generador = generador;
        _hasher = hasher;
        _clock = clock;
        _vivo = vivo;
    }

    public async Task<Result<LlaveFirmaResponse>> CrearLlaveAsync(Guid usuarioId, string password, string? ip, CancellationToken cancellationToken)
    {
        if (!await EsSuperusuarioAsync(usuarioId, password, cancellationToken))
        {
            await RechazarAsync(usuarioId, AccionesApkStore.LlaveCrear, ip, cancellationToken);
            return Result<LlaveFirmaResponse>.Fail(ApkStoreMessages.SoloSuperusuario, 403);
        }

        if (await _db.LlavesFirmaApk.AnyAsync(cancellationToken))
        {
            return Result<LlaveFirmaResponse>.Fail(ApkStoreMessages.LlaveYaExiste, 409);
        }

        var ahora = _clock.UtcNow;
        var generada = _generador.Generar(ApkStoreLimites.AliasLlave, ahora);
        var llave = new LlaveFirmaApk
        {
            LlaveFirmaId = Guid.NewGuid(),
            Alias = ApkStoreLimites.AliasLlave,
            Almacen = generada.Almacen,
            Clave = generada.Clave,
            Huella = generada.Huella,
            CreadaPor = usuarioId,
            FechaCreacionUtc = ahora
        };
        _db.LlavesFirmaApk.Add(llave);
        Auditar(usuarioId, AccionesApkStore.LlaveCrear, $"Huella {llave.Huella}", ip);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<LlaveFirmaResponse>.Created(MapearLlave(llave), ApkStoreMessages.LlaveCreada);
    }

    public async Task<Result<LlaveFirmaDescargaResponse>> DescargarLlaveAsync(Guid usuarioId, string password, string? ip, CancellationToken cancellationToken)
    {
        if (!await EsSuperusuarioAsync(usuarioId, password, cancellationToken))
        {
            await RechazarAsync(usuarioId, AccionesApkStore.LlaveDescargar, ip, cancellationToken);
            return Result<LlaveFirmaDescargaResponse>.Fail(ApkStoreMessages.SoloSuperusuario, 403);
        }

        var llave = await _db.LlavesFirmaApk.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (llave is null)
        {
            return Result<LlaveFirmaDescargaResponse>.Fail(ApkStoreMessages.SinLlave, 404);
        }

        Auditar(usuarioId, AccionesApkStore.LlaveDescargar, null, ip);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<LlaveFirmaDescargaResponse>.Ok(new LlaveFirmaDescargaResponse
        {
            Alias = llave.Alias,
            Almacen = Convert.ToBase64String(llave.Almacen),
            Clave = llave.Clave,
            Huella = llave.Huella
        }, ApkStoreMessages.LlaveEntregada);
    }

    public async Task<Result<LlaveFirmaResponse>> ObtenerHuellaAsync(CancellationToken cancellationToken)
    {
        var llave = await _db.LlavesFirmaApk.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return llave is null
            ? Result<LlaveFirmaResponse>.Fail(ApkStoreMessages.SinLlave, 404)
            : Result<LlaveFirmaResponse>.Ok(MapearLlave(llave), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<VersionApkResponse>> PublicarAsync(
        Guid usuarioId,
        Stream contenido,
        long tamano,
        string nombreOriginal,
        string nombreVersion,
        int numeroCompilacion,
        string? ip,
        CancellationToken cancellationToken)
    {
        if (tamano <= 0)
        {
            return Result<VersionApkResponse>.Fail(VersionAplicacionMessages.ArchivoVacio);
        }

        if (tamano > VersionAplicacionLimites.TamanoMaximoBytes)
        {
            return Result<VersionApkResponse>.Fail(VersionAplicacionMessages.ArchivoDemasiadoGrande);
        }

        if (!ActualizacionAplicacion.EsApk(nombreOriginal))
        {
            return Result<VersionApkResponse>.Fail(VersionAplicacionMessages.NoEsApk);
        }

        if (!ActualizacionAplicacion.NombreVersionValido(nombreVersion))
        {
            return Result<VersionApkResponse>.Fail(VersionAplicacionMessages.NombreInvalido);
        }

        if (numeroCompilacion <= 0)
        {
            return Result<VersionApkResponse>.Fail(VersionAplicacionMessages.CompilacionInvalida);
        }

        var llave = await _db.LlavesFirmaApk.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (llave is null)
        {
            return Result<VersionApkResponse>.Fail(ApkStoreMessages.SinLlave, 409);
        }

        var existentes = await _db.VersionesApk.ToListAsync(cancellationToken);
        if (existentes.Count > 0 && numeroCompilacion <= existentes.Max(v => v.NumeroCompilacion))
        {
            return Result<VersionApkResponse>.Fail(ApkStoreMessages.NumeroNoMayor, 409);
        }

        await using var copia = contenido.CanSeek ? null : await CopiarAsync(contenido, cancellationToken);
        var apk = copia ?? contenido;
        apk.Position = 0;

        var huella = LectorFirmaApk.Huella(apk);
        if (huella is null)
        {
            return Result<VersionApkResponse>.Fail(ApkStoreMessages.SinFirma);
        }

        if (!string.Equals(huella, llave.Huella, StringComparison.OrdinalIgnoreCase))
        {
            await RechazarAsync(usuarioId, AccionesApkStore.VersionPublicar, ip, cancellationToken);
            return Result<VersionApkResponse>.Fail(ApkStoreMessages.FirmaDistinta);
        }

        var sha256 = Convert.ToHexString(await SHA256.HashDataAsync(apk, cancellationToken));
        apk.Position = 0;

        var archivo = ApkStoreLimites.NombreArchivo(numeroCompilacion);
        try
        {
            await _almacen.GuardarAsync(archivo, apk, apk.Length, cancellationToken);
        }
        catch (ApkAlmacenException ex)
        {
            return Result<VersionApkResponse>.Fail(ex.Message, 503);
        }

        var sobrantes = existentes
            .OrderByDescending(v => v.NumeroCompilacion)
            .Skip(ApkStoreLimites.MaximoConservadas - 1)
            .ToList();
        foreach (var vieja in sobrantes)
        {
            try
            {
                await _almacen.EliminarAsync(vieja.NombreArchivo, cancellationToken);
            }
            catch (ApkAlmacenException)
            {
                // Un archivo viejo que no se pudo borrar no impide publicar; queda huérfano en el almacén.
            }

            _db.VersionesApk.Remove(vieja);
        }

        var nueva = new VersionApk
        {
            VersionId = Guid.NewGuid(),
            NumeroCompilacion = numeroCompilacion,
            NombreVersion = nombreVersion.Trim(),
            NombreArchivo = archivo,
            TamanoBytes = apk.Length,
            Sha256 = sha256,
            HuellaFirma = huella,
            PublicadaPor = usuarioId,
            FechaPublicacionUtc = _clock.UtcNow
        };
        _db.VersionesApk.Add(nueva);
        Auditar(usuarioId, AccionesApkStore.VersionPublicar, $"Compilación {numeroCompilacion} ({nueva.NombreVersion})", ip);
        await _db.SaveChangesAsync(cancellationToken);

        var respuesta = MapearVersion(nueva);
        await _vivo.AvisarVersionPublicadaAsync(respuesta, cancellationToken);
        return Result<VersionApkResponse>.Created(respuesta, ApkStoreMessages.Publicada);
    }

    public async Task<Result<IReadOnlyList<VersionApkResponse>>> ListarVersionesAsync(CancellationToken cancellationToken)
    {
        var versiones = await _db.VersionesApk.AsNoTracking()
            .OrderByDescending(v => v.NumeroCompilacion)
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<VersionApkResponse>>.Ok(versiones.Select(MapearVersion).ToList(), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<IReadOnlyList<AuditoriaApkResponse>>> ListarAuditoriaAsync(CancellationToken cancellationToken)
    {
        var registros = await _db.AuditoriaApk.AsNoTracking()
            .OrderByDescending(a => a.FechaUtc)
            .Take(ApkStoreLimites.AuditoriaVisible)
            .ToListAsync(cancellationToken);
        var ids = registros.Select(a => a.UsuarioId).Distinct().ToList();
        var nombres = await _db.Usuarios.AsNoTracking()
            .Where(u => ids.Contains(u.UsuarioId))
            .ToDictionaryAsync(u => u.UsuarioId, u => u.NombreCompleto, cancellationToken);

        var lista = registros.Select(a => new AuditoriaApkResponse
        {
            UsuarioId = a.UsuarioId,
            Usuario = nombres.GetValueOrDefault(a.UsuarioId) ?? a.UsuarioId.ToString(),
            Accion = a.Accion,
            Detalle = a.Detalle,
            Ip = a.Ip,
            FechaUtc = a.FechaUtc
        }).ToList();
        return Result<IReadOnlyList<AuditoriaApkResponse>>.Ok(lista, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<VersionApkResponse>> ObtenerVigenteAsync(CancellationToken cancellationToken)
    {
        var vigente = await VigenteAsync(cancellationToken);
        return vigente is null
            ? Result<VersionApkResponse>.Fail(VersionAplicacionMessages.SinVersion, 404)
            : Result<VersionApkResponse>.Ok(MapearVersion(vigente), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<ApkDescargaResponse>> AbrirVigenteAsync(CancellationToken cancellationToken)
    {
        var vigente = await VigenteAsync(cancellationToken);
        if (vigente is null)
        {
            return Result<ApkDescargaResponse>.Fail(VersionAplicacionMessages.SinVersion, 404);
        }

        Stream? contenido;
        try
        {
            contenido = await _almacen.AbrirAsync(vigente.NombreArchivo, cancellationToken);
        }
        catch (ApkAlmacenException ex)
        {
            return Result<ApkDescargaResponse>.Fail(ex.Message, 503);
        }

        if (contenido is null)
        {
            return Result<ApkDescargaResponse>.Fail(VersionAplicacionMessages.ArchivoNoEncontrado, 404);
        }

        return Result<ApkDescargaResponse>.Ok(new ApkDescargaResponse
        {
            NombreDescarga = ActualizacionAplicacion.NombreDescarga(vigente.NombreVersion, vigente.NumeroCompilacion),
            Contenido = contenido
        }, SuccessMessages.OperacionExitosa);
    }

    private Task<VersionApk?> VigenteAsync(CancellationToken cancellationToken) =>
        _db.VersionesApk.AsNoTracking()
            .OrderByDescending(v => v.NumeroCompilacion)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<bool> EsSuperusuarioAsync(Guid usuarioId, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        var usuario = await _db.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);
        return usuario is { Rol: RolUsuario.Super, Estado: EstadoUsuario.Activo }
            && _hasher.Verify(password, usuario.PasswordHash, usuario.PasswordSalt);
    }

    private async Task RechazarAsync(Guid usuarioId, string accion, string? ip, CancellationToken cancellationToken)
    {
        Auditar(usuarioId, AccionesApkStore.Rechazada, accion, ip);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private void Auditar(Guid usuarioId, string accion, string? detalle, string? ip) =>
        _db.AuditoriaApk.Add(new AuditoriaApk
        {
            AuditoriaId = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Accion = accion,
            Detalle = detalle,
            Ip = ip is { Length: > 64 } ? ip[..64] : ip,
            FechaUtc = _clock.UtcNow
        });

    private static async Task<Stream> CopiarAsync(Stream origen, CancellationToken cancellationToken)
    {
        var temporal = new FileStream(
            Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);
        await origen.CopyToAsync(temporal, cancellationToken);
        return temporal;
    }

    private static LlaveFirmaResponse MapearLlave(LlaveFirmaApk llave) => new()
    {
        Alias = llave.Alias,
        Huella = llave.Huella,
        FechaCreacionUtc = llave.FechaCreacionUtc
    };

    private static VersionApkResponse MapearVersion(VersionApk version) => new()
    {
        VersionId = version.VersionId,
        NumeroCompilacion = version.NumeroCompilacion,
        NombreVersion = version.NombreVersion,
        TamanoBytes = version.TamanoBytes,
        Sha256 = version.Sha256,
        HuellaFirma = version.HuellaFirma,
        FechaPublicacionUtc = version.FechaPublicacionUtc
    };
}
