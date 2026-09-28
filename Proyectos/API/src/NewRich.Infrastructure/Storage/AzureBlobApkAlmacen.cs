using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NewRich.Application.Abstractions;
using NewRich.Constants.Messages;
using NewRich.Shared.Results;

namespace NewRich.Infrastructure.Storage;

public static class OpcionesAzureBlob
{
    public const string ClaveConexion = "AzureBlobConnection";
    public const string ClaveContenedor = "AzureBlobContainerName";

    public static Result<(string Conexion, string Contenedor)> Leer(IConfiguration configuration)
    {
        var conexion = configuration[ClaveConexion];
        var contenedor = configuration[ClaveContenedor];
        if (string.IsNullOrWhiteSpace(conexion))
        {
            return Result<(string Conexion, string Contenedor)>.Fail(
                string.Format(VersionAplicacionMessages.ConfiguracionFaltante, ClaveConexion));
        }

        if (string.IsNullOrWhiteSpace(contenedor))
        {
            return Result<(string Conexion, string Contenedor)>.Fail(
                string.Format(VersionAplicacionMessages.ConfiguracionFaltante, ClaveContenedor));
        }

        return Result<(string Conexion, string Contenedor)>.Ok((conexion, contenedor), SuccessMessages.OperacionExitosa);
    }
}

public sealed class AzureBlobApkAlmacen : IApkAlmacen
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AzureBlobApkAlmacen> _logger;
    private BlobContainerClient? _contenedor;

    public AzureBlobApkAlmacen(IConfiguration configuration, ILogger<AzureBlobApkAlmacen> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task GuardarAsync(string nombreArchivo, Stream contenido, long tamano, CancellationToken cancellationToken)
    {
        _ = tamano;
        try
        {
            var blob = (await ContenedorAsync(cancellationToken)).GetBlobClient(nombreArchivo);
            if (contenido.CanSeek)
            {
                contenido.Position = 0;
            }

            await blob.UploadAsync(
                contenido,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = "application/vnd.android.package-archive"
                    },
                    TransferOptions = CargaBlobApk.Transferencia()
                },
                cancellationToken);
        }
        catch (ApkAlmacenException)
        {
            throw;
        }
        catch (RequestFailedException ex)
        {
            throw Controlado(ex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo completar la carga del APK");
            throw new ApkAlmacenException(VersionAplicacionMessages.NoSePudoGuardar);
        }
    }

    public async Task EliminarAsync(string nombreArchivo, CancellationToken cancellationToken)
    {
        try
        {
            var blob = (await ContenedorAsync(cancellationToken)).GetBlobClient(nombreArchivo);
            await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
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
            var blob = (await ContenedorAsync(cancellationToken)).GetBlobClient(nombreArchivo);
            if (!await blob.ExistsAsync(cancellationToken))
            {
                return null;
            }

            return await blob.OpenReadAsync(cancellationToken: cancellationToken);
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

    private async Task<BlobContainerClient> ContenedorAsync(CancellationToken cancellationToken)
    {
        var contenedor = Cliente();
        if (!await contenedor.ExistsAsync(cancellationToken))
        {
            throw new ApkAlmacenException(VersionAplicacionMessages.RecursoNoDisponible);
        }

        return contenedor;
    }

    private BlobContainerClient Cliente()
    {
        if (_contenedor is not null)
        {
            return _contenedor;
        }

        var leido = OpcionesAzureBlob.Leer(_configuration);
        if (!leido.IsSuccess)
        {
            throw new ApkAlmacenException(leido.Message);
        }

        _contenedor = new BlobContainerClient(leido.Data.Conexion, leido.Data.Contenedor, CargaBlobApk.Opciones());
        return _contenedor;
    }

    private ApkAlmacenException Controlado(RequestFailedException ex)
    {
        _logger.LogWarning("El contenedor de blobs respondió {Status} {ErrorCode}", ex.Status, ex.ErrorCode);
        var mensaje = ex.Status is 401 or 403 or 404
            ? VersionAplicacionMessages.RecursoNoDisponible
            : VersionAplicacionMessages.NoSePudoGuardar;
        return new ApkAlmacenException(mensaje);
    }
}

public static class CargaBlobApk
{
    public const int TamanoBloque = 4 * 1024 * 1024;
    public static readonly TimeSpan TiempoMaximoRed = TimeSpan.FromMinutes(10);

    public static BlobClientOptions Opciones()
    {
        var opciones = new BlobClientOptions();
        opciones.Retry.NetworkTimeout = TiempoMaximoRed;
        opciones.Retry.MaxRetries = 3;
        return opciones;
    }

    public static StorageTransferOptions Transferencia() => new()
    {
        InitialTransferSize = TamanoBloque,
        MaximumTransferSize = TamanoBloque
    };
}
