using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Versiones;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IVersionAplicacionService
{
    Task<Result<VersionAplicacionResponse>> PublicarAsync(
        Stream contenido,
        long tamano,
        string nombreOriginal,
        string nombreVersion,
        int numeroCompilacion,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<VersionAplicacionResponse>>> ListarAsync(CancellationToken cancellationToken);

    Task<Result<VersionAplicacionResponse>> ObtenerVigenteAsync(CancellationToken cancellationToken);

    Task<Result<ApkDescargaResponse>> AbrirVigenteAsync(CancellationToken cancellationToken);
}

public sealed class VersionAplicacionService : IVersionAplicacionService
{
    private readonly INewRichDbContext _db;
    private readonly IApkAlmacen _almacen;
    private readonly IClock _clock;
    private readonly IVersionesTiempoReal _vivo;

    public VersionAplicacionService(INewRichDbContext db, IApkAlmacen almacen, IClock clock, IVersionesTiempoReal vivo)
    {
        _db = db;
        _almacen = almacen;
        _clock = clock;
        _vivo = vivo;
    }

    public async Task<Result<VersionAplicacionResponse>> PublicarAsync(
        Stream contenido,
        long tamano,
        string nombreOriginal,
        string nombreVersion,
        int numeroCompilacion,
        CancellationToken cancellationToken)
    {
        if (tamano <= 0)
        {
            return Result<VersionAplicacionResponse>.Fail(VersionAplicacionMessages.ArchivoVacio);
        }

        if (tamano > VersionAplicacionLimites.TamanoMaximoBytes)
        {
            return Result<VersionAplicacionResponse>.Fail(VersionAplicacionMessages.ArchivoDemasiadoGrande);
        }

        if (!ActualizacionAplicacion.EsApk(nombreOriginal))
        {
            return Result<VersionAplicacionResponse>.Fail(VersionAplicacionMessages.NoEsApk);
        }

        if (!ActualizacionAplicacion.NombreVersionValido(nombreVersion))
        {
            return Result<VersionAplicacionResponse>.Fail(VersionAplicacionMessages.NombreInvalido);
        }

        if (numeroCompilacion <= 0)
        {
            return Result<VersionAplicacionResponse>.Fail(VersionAplicacionMessages.CompilacionInvalida);
        }

        var existentes = await _db.VersionesAplicacion.ToListAsync(cancellationToken);
        if (existentes.Any(v => v.NumeroCompilacion == numeroCompilacion))
        {
            return Result<VersionAplicacionResponse>.Fail(VersionAplicacionMessages.CompilacionRepetida, 409);
        }

        var archivo = ActualizacionAplicacion.NombreArchivo(numeroCompilacion);
        var antigua = existentes.Count >= ActualizacionAplicacion.MaximoConservadas
            ? existentes.OrderBy(v => v.FechaPublicacion).ThenBy(v => v.NumeroCompilacion).First()
            : null;

        try
        {
            await _almacen.GuardarAsync(archivo, contenido, tamano, cancellationToken);
        }
        catch (ApkAlmacenException ex)
        {
            return Result<VersionAplicacionResponse>.Fail(ex.Message, 503);
        }

        if (antigua is not null)
        {
            try
            {
                await _almacen.EliminarAsync(antigua.NombreArchivo, cancellationToken);
            }
            catch (ApkAlmacenException ex)
            {
                await IntentarEliminarAsync(archivo, cancellationToken);
                return Result<VersionAplicacionResponse>.Fail(ex.Message, 503);
            }
        }

        var nueva = new VersionAplicacion
        {
            VersionAplicacionId = Guid.NewGuid(),
            NumeroCompilacion = numeroCompilacion,
            NombreVersion = nombreVersion.Trim(),
            NombreArchivo = archivo,
            TamanoBytes = tamano,
        FechaPublicacion = _clock.LocalNow
        };

        if (antigua is not null)
        {
            _db.VersionesAplicacion.Remove(antigua);
        }

        _db.VersionesAplicacion.Add(nueva);
        await _db.SaveChangesAsync(cancellationToken);

        var respuesta = Mapear(nueva, true);
        await _vivo.AvisarVersionPublicadaAsync(respuesta, cancellationToken);
        return Result<VersionAplicacionResponse>.Created(respuesta, VersionAplicacionMessages.Publicada);
    }

    public async Task<Result<IReadOnlyList<VersionAplicacionResponse>>> ListarAsync(CancellationToken cancellationToken)
    {
        var existentes = await _db.VersionesAplicacion.ToListAsync(cancellationToken);
        var vigenteId = ElegirVigente(existentes)?.VersionAplicacionId;
        var lista = existentes
            .OrderByDescending(v => v.FechaPublicacion)
            .ThenByDescending(v => v.NumeroCompilacion)
            .Select(v => Mapear(v, v.VersionAplicacionId == vigenteId))
            .ToList();
        return Result<IReadOnlyList<VersionAplicacionResponse>>.Ok(lista, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<VersionAplicacionResponse>> ObtenerVigenteAsync(CancellationToken cancellationToken)
    {
        var vigentes = await _db.VersionesAplicacion.ToListAsync(cancellationToken);
        var vigente = ElegirVigente(vigentes);
        if (vigente is null)
        {
            return Result<VersionAplicacionResponse>.Fail(VersionAplicacionMessages.SinVersion, 404);
        }

        return Result<VersionAplicacionResponse>.Ok(Mapear(vigente, true), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<ApkDescargaResponse>> AbrirVigenteAsync(CancellationToken cancellationToken)
    {
        var vigentes = await _db.VersionesAplicacion.ToListAsync(cancellationToken);
        var vigente = ElegirVigente(vigentes);
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

    private async Task IntentarEliminarAsync(string nombreArchivo, CancellationToken cancellationToken)
    {
        try
        {
            await _almacen.EliminarAsync(nombreArchivo, cancellationToken);
        }
        catch (ApkAlmacenException)
        {
        }
    }

    private static VersionAplicacion? ElegirVigente(IReadOnlyList<VersionAplicacion> versiones) =>
        versiones
            .OrderByDescending(v => v.FechaPublicacion)
            .ThenByDescending(v => v.NumeroCompilacion)
            .FirstOrDefault();

    private static VersionAplicacionResponse Mapear(VersionAplicacion version, bool vigente) => new()
    {
        VersionAplicacionId = version.VersionAplicacionId,
        NumeroCompilacion = version.NumeroCompilacion,
        NombreVersion = version.NombreVersion,
        TamanoBytes = version.TamanoBytes,
        FechaPublicacion = version.FechaPublicacion,
        Vigente = vigente
    };
}
