using System.Net;
using System.Text;
using FluentAssertions;
using NewRich.Application.Contracts.Auth;
using NewRich.Pda.Core.Api;

namespace NewRich.Pda.Tests;

public sealed class NewRichApiClientTests
{
    [Fact]
    public async Task Conectar_usa_la_primera_url_que_responde()
    {
        var handler = new StubHandler("{}", HttpStatusCode.OK);
        var opciones = new ApiOpciones { BaseUrl = "http://falla:1/" };
        var client = new NewRichApiClient(new HttpClient(handler), new MemoriaTokens(), opciones);

        var resultado = await client.ConectarAsync(["http://localhost:5295/"], CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        opciones.BaseUrl.Should().Be("http://localhost:5295/");
        handler.UltimaRuta.Should().Contain("swagger/v1/swagger.json");
    }

    [Fact]
    public async Task Login_lee_success_message_y_token()
    {
        var handler = new StubHandler("""
            {"success":true,"message":"ok","data":{"token":"abc","usuarioId":"11111111-1111-1111-1111-111111111111","nombreUsuario":"crojas","nombreCompleto":"Camila","rol":2,"debeCambiarPassword":true,"fechaExpiracion":"2026-09-09T20:00:00Z"}}
            """);
        var tokens = new MemoriaTokens();
        var client = new NewRichApiClient(new HttpClient(handler), tokens, new ApiOpciones { BaseUrl = "http://localhost:5295/" });

        var resultado = await client.LoginAsync(new() { Usuario = "crojas", Password = "x", CodigoDispositivo = "PDA-042" }, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Data!.Token.Should().Be("abc");
        resultado.Data.DebeCambiarPassword.Should().BeTrue();
        handler.UltimaRuta.Should().EndWith("api/AuthAndroid/LoginMob");
    }

    private sealed class MemoriaTokens : ITokenStore
    {
        private string? _token;
        public Task BorrarAsync() { _token = null; return Task.CompletedTask; }
        public Task GuardarAsync(string token) { _token = token; return Task.CompletedTask; }
        public Task<string?> ObtenerAsync() => Task.FromResult(_token);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _json;
        public string UltimaRuta { get; private set; } = string.Empty;

        public StubHandler(string json, HttpStatusCode _ = HttpStatusCode.OK) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            UltimaRuta = request.RequestUri?.PathAndQuery ?? string.Empty;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            });
        }
    }
}
