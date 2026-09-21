using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Constants;

namespace NewRich.Admin.Tests;

public sealed class ConfirmacionTokenAdminApiTests
{
    [Fact]
    public async Task Reenvia_el_token_de_confirmacion_del_administrador_a_la_api()
    {
        HttpRequestMessage? enviada = null;
        var contexto = new DefaultHttpContext();
        contexto.Request.Headers[ConfirmacionAccion.HeaderToken] = "token-lote-1";
        var sut = CrearCliente(new Captura(msg => enviada = msg, OkUsuario()), contexto);

        await sut.CrearUsuarioAsync(new CrearUsuarioRequest { NombreCompleto = "Ana", Usuario = "ana" }, CancellationToken.None);

        enviada.Should().NotBeNull();
        enviada!.Headers.TryGetValues(ConfirmacionAccion.HeaderToken, out var valores).Should().BeTrue();
        valores.Should().Contain("token-lote-1");
    }

    private static AdminApiClient CrearCliente(HttpMessageHandler manejador, HttpContext contexto)
    {
        var http = new HttpClient(manejador) { BaseAddress = new Uri("http://localhost:5295/") };
        return new AdminApiClient(http, new HttpContextAccessor { HttpContext = contexto });
    }

    private static HttpResponseMessage OkUsuario() =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"success":true,"message":"Listo.","data":{"usuarioId":"00000000-0000-0000-0000-000000000001","usuario":"ana","nombreCompleto":"Ana"}}""",
                Encoding.UTF8,
                "application/json")
        };

    private sealed class Captura : HttpMessageHandler
    {
        private readonly Action<HttpRequestMessage> _capturar;
        private readonly HttpResponseMessage _respuesta;

        public Captura(Action<HttpRequestMessage> capturar, HttpResponseMessage respuesta)
        {
            _capturar = capturar;
            _respuesta = respuesta;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _capturar(request);
            return Task.FromResult(_respuesta);
        }
    }
}
