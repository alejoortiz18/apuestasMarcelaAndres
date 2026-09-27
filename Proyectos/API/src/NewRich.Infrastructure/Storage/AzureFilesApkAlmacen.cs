using Azure;
using Azure.Storage.Files.Shares;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NewRich.Application.Abstractions;
using NewRich.Constants.Messages;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Infrastructure.Storage;

public static class OpcionesAzureFiles
{
    public const string ClaveConexion = "AzureFilesConnection";
    public const string ClaveRecurso = "AzureFilesShareName";

    public static Result<(string Conexion, string Recurso)> Leer(IConfiguration configuration)
    {
        var conexion = configuration[ClaveConexion];
        var recurso = configuration[ClaveRecurso];
        if (string.IsNullOrWhiteSpace(conexion))
        {
            return Result<(string Conexion, string Recurso)>.Fail(
                string.Format(VersionAplicacionMessages.ConfiguracionFaltante, ClaveConexion));
        }

        if (string.IsNullOrWhiteSpace(recurso))
        {
            return Result<(string Conexion, string Recurso)>.Fail(
                string.Format(VersionAplicacionMessages.ConfiguracionFaltante, ClaveRecurso));
        }

        return Result<(string Conexion, string Recurso)>.Ok((conexion, recurso), SuccessMessages.OperacionExitosa);
    }
}

public sealed class AzureFilesApkAlmacen : IApkAlmacen
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureFilesApkAlmacen> _logger;
    private ShareClient? _recurso;

    public AzureFilesApkAlmacen(IConfiguration configuration, ILogger<AzureFilesApkAlmacen> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task GuardarAsync(string nombreArchivo, Stream contenido, long tamano, CancellationToken cancellationToken)
    {
        try
        {
            var directorio = await DirectorioAsync(cancellationToken);
            var archivo = directorio.GetFileClient(nombreArchivo);
            await archivo.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            await archivo.CreateAsync(tamano, cancellationToken: cancellationToken);
            if (contenido.CanSeek)
            {
                contenido.Position = 0;
            }

            await archivo.UploadRangeAsync(new HttpRange(0, tamano), contenido, cancellationToken: cancellationToken);
        }
        catch (ApkAlmacenException)
        {
            throw;
        }
        catch (RequestFailedException ex)
        {
            throw Controlado(ex);
        }
    }

    public async Task EliminarAsync(string nombreArchivo, CancellationToken cancellationToken)
    {
        try
        {
            var directorio = await DirectorioAsync(cancellationToken);
            await directorio.GetFileClient(nombreArchivo).DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch (ApkAlmacenException)
        {
            throw;
        }
        catch (RequestFailedException ex)
        {
            throw Controlado(ex);
        }
    }

    public async Task<Stream?> AbrirAsync(string nombreArchivo, CancellationToken cancellationToken)
    {
        try
        {
            var archivo = (await DirectorioAsync(cancellationToken)).GetFileClient(nombreArchivo);
            if (!await archivo.ExistsAsync(cancellationToken))
            {
                return null;
            }

            return await archivo.OpenReadAsync(cancellationToken: cancellationToken);
        }
        catch (ApkAlmacenException)
        {
            throw;
        }
        catch (RequestFailedException ex)
        {
            throw Controlado(ex);
        }
    }

    private async Task<ShareDirectoryClient> DirectorioAsync(CancellationToken cancellationToken)
    {
        var recurso = Recurso();
        if (!await recurso.ExistsAsync(cancellationToken))
        {
            throw new ApkAlmacenException(VersionAplicacionMessages.RecursoNoDisponible);
        }

        var directorio = recurso.GetDirectoryClient(ActualizacionAplicacion.Directorio);
        await directorio.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        return directorio;
    }

    private ShareClient Recurso()
    {
        if (_recurso is not null)
        {
            return _recurso;
        }

        var leido = OpcionesAzureFiles.Leer(_configuration);
        if (!leido.IsSuccess)
        {
            throw new ApkAlmacenException(leido.Message);
        }

        _recurso = new ShareClient(leido.Data.Conexion, leido.Data.Recurso);
        return _recurso;
    }

    private ApkAlmacenException Controlado(RequestFailedException ex)
    {
        _logger.LogWarning("Azure Files respondió {Status} {ErrorCode}", ex.Status, ex.ErrorCode);
        var mensaje = ex.Status is 401 or 403 or 404
            ? VersionAplicacionMessages.RecursoNoDisponible
            : VersionAplicacionMessages.NoSePudoGuardar;
        return new ApkAlmacenException(mensaje);
    }
}
