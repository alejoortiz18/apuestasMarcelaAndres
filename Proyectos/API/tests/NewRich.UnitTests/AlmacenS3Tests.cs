using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NewRich.Application.Abstractions;
using NewRich.Infrastructure;
using NewRich.Infrastructure.Storage;

namespace NewRich.UnitTests;

public sealed class AlmacenS3Tests
{
    [Fact]
    public void Con_bucket_configurado_los_archivos_van_a_S3()
    {
        using var proveedor = Proveedor(new Dictionary<string, string?>
        {
            [OpcionesAwsS3.ClaveBucket] = "bucket-prueba",
            [OpcionesAwsS3.ClaveRegion] = "us-east-2"
        });

        proveedor.GetRequiredService<IApkAlmacen>().Should().BeOfType<S3ApkAlmacen>();
        proveedor.GetRequiredService<IChatFileStorage>().Should().BeOfType<S3ChatFileStorage>();
    }

    [Fact]
    public void Sin_bucket_se_conserva_Azure_Blob_y_el_disco_local()
    {
        using var proveedor = Proveedor(new Dictionary<string, string?>
        {
            ["Storage:Chat"] = Path.Combine(Path.GetTempPath(), "newrich-chat-pruebas")
        });

        proveedor.GetRequiredService<IApkAlmacen>().Should().BeOfType<AzureBlobApkAlmacen>();
        proveedor.GetRequiredService<IChatFileStorage>().Should().BeOfType<LocalChatFileStorage>();
    }

    [Theory]
    [InlineData("apks/", "12.apk", "apks/12.apk")]
    [InlineData("chat-files/", "20261015/abc.jpg", "chat-files/20261015/abc.jpg")]
    [InlineData("chat-files/", "20261015\\abc.jpg", "chat-files/20261015/abc.jpg")]
    [InlineData("chat-files/", "/20261015/abc.jpg", "chat-files/20261015/abc.jpg")]
    public void La_llave_une_prefijo_y_ruta_con_barras_normales(string prefijo, string ruta, string esperada)
    {
        OpcionesAwsS3.Llave(prefijo, ruta).Should().Be(esperada);
    }

    [Fact]
    public void El_flujo_informa_la_longitud_para_que_la_descarga_lleve_Content_Length()
    {
        using var flujo = new FlujoConLongitud(new MemoryStream(new byte[10]), 10, null);

        flujo.CanSeek.Should().BeTrue();
        flujo.Length.Should().Be(10);
        flujo.Position.Should().Be(0);
    }

    [Fact]
    public async Task El_flujo_avanza_la_posicion_al_leer_y_solo_acepta_buscar_donde_ya_esta()
    {
        await using var flujo = new FlujoConLongitud(new MemoryStream(new byte[10]), 10, null);

        var leidos = await flujo.ReadAsync(new byte[4]);

        leidos.Should().Be(4);
        flujo.Position.Should().Be(4);
        flujo.Seek(0, SeekOrigin.Current).Should().Be(4);
        flujo.Invoking(f => f.Seek(0, SeekOrigin.Begin)).Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Al_cerrar_el_flujo_se_libera_la_respuesta_de_S3()
    {
        var respuesta = new Liberable();

        new FlujoConLongitud(new MemoryStream(), 0, respuesta).Dispose();

        respuesta.Liberado.Should().BeTrue();
    }

    private static ServiceProvider Proveedor(Dictionary<string, string?> valores)
    {
        valores["ConnectionStrings:NewRichDatabase"] = "Server=.;Database=Pruebas;Trusted_Connection=True;";
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        var servicios = new ServiceCollection();
        servicios.AddSingleton<IConfiguration>(configuracion);
        servicios.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        servicios.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        servicios.AddInfrastructure(configuracion);
        return servicios.BuildServiceProvider();
    }

    private sealed class Liberable : IDisposable
    {
        public bool Liberado { get; private set; }

        public void Dispose() => Liberado = true;
    }
}
