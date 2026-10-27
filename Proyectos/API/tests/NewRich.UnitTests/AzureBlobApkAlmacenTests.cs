using System.Net;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NewRich.Infrastructure.Storage;

namespace NewRich.UnitTests;

public sealed class AzureBlobApkAlmacenTests
{
    private const string ConexionSas =
        "BlobEndpoint=https://cuenta.blob.core.windows.net/;SharedAccessSignature=sp=racwd&sv=2026-02-06&sr=c&sig=firma";

    [Fact]
    public async Task Con_token_del_contenedor_sube_el_apk_sin_consultar_el_contenedor()
    {
        var azure = new AzureSoloBlobs();
        var sut = Crear(azure);

        await sut.GuardarAsync("newrich-1.0-2.apk", new MemoryStream([1, 2, 3]), 3, CancellationToken.None);

        azure.Consultas.Should().ContainSingle()
            .Which.Should().Be("PUT /produccion/newrich-1.0-2.apk");
    }

    [Fact]
    public async Task Con_token_del_contenedor_abre_el_apk_y_devuelve_null_si_no_existe()
    {
        var sut = Crear(new AzureSoloBlobs());

        var existente = await sut.AbrirAsync("newrich-1.0-2.apk", CancellationToken.None);
        var faltante = await sut.AbrirAsync("no-existe.apk", CancellationToken.None);

        existente.Should().NotBeNull();
        faltante.Should().BeNull();
    }

    private static AzureBlobApkAlmacen Crear(HttpMessageHandler azure)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [OpcionesAzureBlob.ClaveConexion] = ConexionSas,
                [OpcionesAzureBlob.ClaveContenedor] = "produccion"
            })
            .Build();
        var opciones = new BlobClientOptions { Transport = new HttpClientTransport(new HttpClient(azure)) };
        opciones.Retry.MaxRetries = 0;
        return new AzureBlobApkAlmacen(config, NullLogger<AzureBlobApkAlmacen>.Instance, opciones);
    }

    // Responde como Azure ante un token SAS de contenedor: niega las operaciones sobre el contenedor
    // y atiende las de cada blob.
    private sealed class AzureSoloBlobs : HttpMessageHandler
    {
        public List<string> Consultas { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var ruta = request.RequestUri!.AbsolutePath;
            Consultas.Add($"{request.Method} {ruta}");
            var esContenedor = request.RequestUri.Query.Contains("restype=container", StringComparison.OrdinalIgnoreCase);
            var estado = esContenedor ? HttpStatusCode.Forbidden
                : ruta.EndsWith("no-existe.apk", StringComparison.Ordinal) ? HttpStatusCode.NotFound
                : request.Method == HttpMethod.Put ? HttpStatusCode.Created
                : HttpStatusCode.OK;
            var respuesta = new HttpResponseMessage(estado) { Content = new ByteArrayContent(estado == HttpStatusCode.OK ? [1, 2, 3] : []) };
            respuesta.Content.Headers.ContentLength = estado == HttpStatusCode.OK ? 3 : 0;
            if (estado == HttpStatusCode.OK)
            {
                respuesta.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"0x1\"");
                respuesta.Content.Headers.LastModified = DateTimeOffset.UtcNow;
                respuesta.Headers.Add("x-ms-blob-type", "BlockBlob");
            }
            if (estado == HttpStatusCode.Created)
            {
                respuesta.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"0x1\"");
                respuesta.Content.Headers.LastModified = DateTimeOffset.UtcNow;
            }
            if (estado is HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
            {
                respuesta.Headers.Add("x-ms-error-code", estado == HttpStatusCode.Forbidden ? "AuthorizationFailure" : "BlobNotFound");
            }
            return Task.FromResult(respuesta);
        }
    }
}
