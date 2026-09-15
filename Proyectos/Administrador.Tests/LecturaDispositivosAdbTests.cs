using FluentAssertions;
using NewRich.Admin.Services.Pda;

namespace NewRich.Admin.Tests;

/// <summary>Lectura de la salida de "adb devices -l" que alimenta el registro guiado.</summary>
public sealed class LecturaDispositivosAdbTests
{
    [Fact]
    public void Detecta_el_pda_conectado_por_usb()
    {
        const string salida = """
            List of devices attached
            TCDUAAVWV4KVGI8L       device product:RMX3710 model:RMX3710 device:RE54C4 transport_id:1
            """;

        var dispositivos = SalidaAdb.LeerDispositivos(salida);

        dispositivos.Should().ContainSingle();
        dispositivos[0].NumeroSerie.Should().Be("TCDUAAVWV4KVGI8L");
        dispositivos[0].Estado.Should().Be(EstadoConexionAdb.Listo);
        dispositivos[0].Modelo.Should().Be("RMX3710");
    }

    [Fact]
    public void Reconoce_el_equipo_que_no_autorizo_la_depuracion_usb()
    {
        const string salida = """
            List of devices attached
            TCDUAAVWV4KVGI8L       unauthorized
            """;

        var dispositivos = SalidaAdb.LeerDispositivos(salida);

        dispositivos.Should().ContainSingle();
        dispositivos[0].Estado.Should().Be(EstadoConexionAdb.SinAutorizar);
    }

    [Fact]
    public void Un_equipo_que_no_responde_no_queda_listo()
    {
        const string salida = """
            List of devices attached
            TCDUAAVWV4KVGI8L       offline
            """;

        SalidaAdb.LeerDispositivos(salida)[0].Estado.Should().Be(EstadoConexionAdb.NoDisponible);
    }

    [Fact]
    public void Ignora_los_emuladores_porque_el_pda_se_conecta_por_usb()
    {
        const string salida = """
            List of devices attached
            emulator-5554          device product:sdk model:Android_SDK transport_id:1
            TCDUAAVWV4KVGI8L       device product:RMX3710 model:RMX3710 transport_id:2
            """;

        var dispositivos = SalidaAdb.LeerDispositivos(salida);

        dispositivos.Should().ContainSingle();
        dispositivos[0].NumeroSerie.Should().Be("TCDUAAVWV4KVGI8L");
    }

    [Fact]
    public void Ignora_los_avisos_del_servicio_adb()
    {
        const string salida = """
            * daemon not running; starting now at tcp:5037
            * daemon started successfully
            List of devices attached

            TCDUAAVWV4KVGI8L       device model:RMX3710
            """;

        SalidaAdb.LeerDispositivos(salida).Should().ContainSingle();
    }

    [Fact]
    public void Sin_equipos_conectados_la_lista_queda_vacia()
    {
        SalidaAdb.LeerDispositivos("List of devices attached\n\n").Should().BeEmpty();
    }
}
