using FluentAssertions;
using NewRich.Admin.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Services;

namespace NewRich.Admin.Tests;

public sealed class SeleccionDiscoUsbAdminTests
{
    [Fact]
    public void Traduce_varias_usb_al_mensaje_de_seleccion()
    {
        InventarioUsbMensajes.De(ResultadoSeleccionUsb.Varias).Should().Be(LlaveMessages.VariasUsb);
        InventarioUsbMensajes.De(ResultadoSeleccionUsb.Ninguna).Should().Be(LlaveMessages.UsbNoDetectada);
    }
}

public sealed class ClaimsSuperTests
{
    [Fact]
    public void Super_con_claim_de_administrador_es_equipo_administrativo()
    {
        var identity = new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Super"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Administrador")
            ],
            "test");
        var user = new System.Security.Claims.ClaimsPrincipal(identity);

        user.EsAdministrador().Should().BeTrue();
    }
}
