using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using NewRich.Application.Abstractions;

namespace NewRich.Infrastructure.Storage;

public sealed class S3ChatFileStorage : IChatFileStorage
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public S3ChatFileStorage(IAmazonS3 s3, IConfiguration configuration)
    {
        _s3 = s3;
        _bucket = OpcionesAwsS3.Bucket(configuration);
    }

    public async Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken)
    {
        var relative = $"{DateTime.UtcNow:yyyyMMdd}/{Guid.NewGuid():N}{Path.GetExtension(originalFileName)}";
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = OpcionesAwsS3.Llave(OpcionesAwsS3.PrefijoChat, relative),
            InputStream = content,
            AutoCloseStream = false
        }, cancellationToken);
        return relative;
    }

    public async Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken)
    {
        try
        {
            var respuesta = await _s3.GetObjectAsync(_bucket, OpcionesAwsS3.Llave(OpcionesAwsS3.PrefijoChat, relativePath), cancellationToken);
            return new FlujoConLongitud(respuesta.ResponseStream, respuesta.ContentLength, respuesta);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
        {
            return null;
        }
    }

    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken) =>
        _s3.DeleteObjectAsync(_bucket, OpcionesAwsS3.Llave(OpcionesAwsS3.PrefijoChat, relativePath), cancellationToken);
}
