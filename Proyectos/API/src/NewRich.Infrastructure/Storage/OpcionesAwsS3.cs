using Amazon;
using Amazon.S3;
using Microsoft.Extensions.Configuration;

namespace NewRich.Infrastructure.Storage;

public static class OpcionesAwsS3
{
    public const string ClaveBucket = "AwsS3:Bucket";
    public const string ClaveRegion = "AwsS3:Region";
    public const string PrefijoApk = "apks/";
    public const string PrefijoChat = "chat-files/";

    public static bool Configurado(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration[ClaveBucket]);

    public static string Bucket(IConfiguration configuration) => configuration[ClaveBucket]!;

    public static string Llave(string prefijo, string ruta) =>
        prefijo + ruta.Replace('\\', '/').TrimStart('/');

    public static IAmazonS3 Cliente(IConfiguration configuration)
    {
        var region = configuration[ClaveRegion];
        var opciones = new AmazonS3Config();
        if (!string.IsNullOrWhiteSpace(region))
        {
            opciones.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
        }

        return new AmazonS3Client(opciones);
    }
}
