using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class LlaveUsbCriptografiaTests
{
    [Fact]
    public void La_copia_a_otra_usb_no_puede_desenvolver_la_clave()
    {
        var material = LlaveUsbCriptografia.Generar("KEY-8F72A91C", "SERIE-A", "VOL-1");

        var ok = LlaveUsbCriptografia.TryDesenvolver(material.SecretoEnvuelto, "SERIE-A", "VOL-1", out var privada);
        ok.Should().BeTrue();
        privada.Should().NotBeEmpty();

        LlaveUsbCriptografia.TryDesenvolver(material.SecretoEnvuelto, "SERIE-B", "VOL-1", out _).Should().BeFalse();
        LlaveUsbCriptografia.TryDesenvolver(material.SecretoEnvuelto, "SERIE-A", "VOL-2", out _).Should().BeFalse();
    }

    [Fact]
    public void La_firma_solo_es_valida_con_la_clave_publica_de_esa_llave()
    {
        var material = LlaveUsbCriptografia.Generar("KEY-71B42D55", "SERIE-A", "VOL-1");
        LlaveUsbCriptografia.TryDesenvolver(material.SecretoEnvuelto, "SERIE-A", "VOL-1", out var privada).Should().BeTrue();

        var payload = LlaveUsbCriptografia.PayloadLogin(material.Codigo, "admin", material.Huella, 1_790_000_000);
        var firma = LlaveUsbCriptografia.Firmar(privada, payload);

        LlaveUsbCriptografia.Verificar(material.ClavePublica, payload, firma).Should().BeTrue();
        LlaveUsbCriptografia.Verificar(material.ClavePublica, payload + "x", firma).Should().BeFalse();
    }
}

public sealed class DiscoExternoUsbTests
{
    [Fact]
    public void Acepta_solo_discos_usb_que_no_son_el_sistema()
    {
        DiscoExternoUsb.EsApto("USB", esDiscoSistema: false).Should().BeTrue();
        DiscoExternoUsb.EsApto("USB", esDiscoSistema: true).Should().BeFalse();
        DiscoExternoUsb.EsApto("NVMe", esDiscoSistema: false).Should().BeFalse();
        DiscoExternoUsb.EsApto("SATA", esDiscoSistema: false).Should().BeFalse();
    }
}
