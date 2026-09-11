using System.Security.Claims;
using FluentAssertions;
using NewRich.Shared;

namespace NewRich.UnitTests;

public sealed class ClaimsUsuarioTests
{
    [Fact]
    public void TryId_lee_sub_aunque_no_este_el_nameid()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "3fa85f64-5717-4562-b3fc-2c963f66afa6")
        ], "jwt"));

        ClaimsUsuario.TryId(user, out var id).Should().BeTrue();
        id.Should().Be(Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6"));
    }

    [Fact]
    public void HubUrl_usa_el_mismo_host_del_navegador_cuando_la_api_esta_en_localhost()
    {
        HubNotificacionesUrl.Resolver("http://localhost:5295/", "127.0.0.1", "/hubs/notificaciones")
            .Should().Be("http://127.0.0.1:5295/hubs/notificaciones");
    }

    [Fact]
    public void HubRutas_acepta_token_de_notificaciones_y_de_chat_sin_mezclarlos()
    {
        HubRutas.AceptaTokenPorQuery("/hubs/notificaciones").Should().BeTrue();
        HubRutas.AceptaTokenPorQuery("/hubs/chat").Should().BeTrue();
        HubRutas.AceptaTokenPorQuery("/api/Ventas").Should().BeFalse();
        HubRutas.EventoNuevaNotificacion.Should().Be("nuevaNotificacion");
        HubRutas.EventoMensajeChat.Should().Be("mensajeChat");
        HubRutas.EventoCodigosOfflineAsignados.Should().Be("codigosOfflineAsignados");
    }
}
