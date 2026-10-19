using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NewRich.Application.Abstractions;
using NewRich.Constants.Messages;

namespace NewRich.Infrastructure.Storage;

public sealed class S3ApkAlmacen : IApkAlmacen
{
    private const long TamanoParte = 8 * 1024 * 1024;

    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly ILogger<S3ApkAlmacen> _logger;

    public S3ApkAlmacen(IAmazonS3 s3, IConfiguration configuration, ILogger<S3ApkAlmacen> logger)
    {
        _s3 = s3;
        _bucket = OpcionesAwsS3.Bucket(configuration);
        _logger = logger;
    }

    public async Task GuardarAsync(string nombreArchivo, Stream contenido, long tamano, CancellationToken cancellationToken)
    {
        _ = tamano;
        try
        {
            if (contenido.CanSeek)
            {
                contenido.Position = 0;
            }

            using var transferencia = new TransferUtility(_s3);
            await transferencia.UploadAsync(new TransferUtilityUploadRequest
            {
                BucketName = _bucket,
                Key = OpcionesAwsS3.Llave(OpcionesAwsS3.PrefijoApk, nombreArchivo),
                InputStream = contenido,
                AutoCloseStream = false,
                ContentType = "application/vnd.android.package-archive",
                PartSize = TamanoParte
            }, cancellationToken);
        }
        catch (AmazonS3Exception ex)
        {
            throw Controlado(ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "No se pudo completar la carga del APK");
            throw new ApkAlmacenException(VersionAplicacionMessages.NoSePudoGuardar);
        }
    }

    public async Task EliminarAsync(string nombreArchivo, CancellationToken cancellationToken)
    {
        try
        {
            await _s3.DeleteObjectAsync(_bucket, OpcionesAwsS3.Llave(OpcionesAwsS3.PrefijoApk, nombreArchivo), cancellationToken);
        }
        catch (AmazonS3Exception ex)
        {
            throw Controlado(ex);
        }
    }

    public async Task<Stream?> AbrirAsync(string nombreArchivo, CancellationToken cancellationToken)
    {
        try
        {
            var respuesta = await _s3.GetObjectAsync(_bucket, OpcionesAwsS3.Llave(OpcionesAwsS3.PrefijoApk, nombreArchivo), cancellationToken);
            return new FlujoConLongitud(respuesta.ResponseStream, respuesta.ContentLength, respuesta);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
        {
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            throw Controlado(ex);
        }
    }

    private ApkAlmacenException Controlado(AmazonS3Exception ex)
    {
        _logger.LogWarning("El bucket de S3 respondió {Status} {ErrorCode}", (int)ex.StatusCode, ex.ErrorCode);
        var mensaje = ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound
            ? VersionAplicacionMessages.RecursoNoDisponible
            : VersionAplicacionMessages.NoSePudoGuardar;
        return new ApkAlmacenException(mensaje);
    }
}
