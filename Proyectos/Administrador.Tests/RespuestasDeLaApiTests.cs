using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NewRich.Admin.Constants;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Resultados;

namespace NewRich.Admin.Tests;

/// <summary>
/// Como traduce el cliente las respuestas de la API. Un servidor que responde con un codigo
/// inesperado no es lo mismo que un servidor apagado, y el mensaje debe distinguirlos.
/// </summary>
public sealed class RespuestasDeLaApiTests
{
    [Fact]
    public async Task Una_respuesta_sin_cuerpo_informa_el_codigo_http_en_vez_de_culpar_a_la_conexion()
    {
        var sut = CrearCliente(new HttpResponseMessage(HttpStatusCode.MethodNotAllowed));

        var resultado = await sut.ListarDispositivosAsync(CancellationToken.None);

        resultado.Success.Should().BeFalse();
        resultado.Message.Should().Contain("405");
        resultado.Message.Should().NotBe(UiTexts.ApiNoDisponible);
    }

    [Fact]
    public async Task Una_ruta_inexistente_no_dice_que_la_api_esta_apagada()
    {
        var sut = CrearCliente(new HttpResponseMessage(HttpStatusCode.NotFound));

        var resultado = await sut.ListarDispositivosAsync(CancellationToken.None);

        resultado.Message.Should().Contain("404");
    }

    [Fact]
    public async Task Sin_servidor_al_otro_lado_si_avisa_que_la_api_no_responde()
    {
        var sut = CrearCliente(new HttpRequestException("sin conexion"));

        var resultado = await sut.ListarDispositivosAsync(CancellationToken.None);

        resultado.Success.Should().BeFalse();
        resultado.Message.Should().Be(UiTexts.ApiNoDisponible);
    }

    [Fact]
    public async Task Una_respuesta_correcta_entrega_los_datos()
    {
        var sut = CrearCliente(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"success":true,"message":"Listo.","data":[]}""", Encoding.UTF8, "application/json")
        });

        var resultado = await sut.ListarDispositivosAsync(CancellationToken.None);

        resultado.Success.Should().BeTrue();
        resultado.Data.Should().NotBeNull();
    }

    [Fact]
    public void Un_resultado_expone_su_cantidad_de_ganadores()
    {
        var resultado = new ResultadoResponse
        {
            CantidadGanadores = 2,
            Numero = "1234"
        };

        resultado.CantidadGanadores.Should().Be(2);
        resultado.TieneGanadores.Should().BeTrue();
    }

    private static AdminApiClient CrearCliente(HttpResponseMessage respuesta) =>
        CrearCliente(new RespuestaFija(respuesta));

    private static AdminApiClient CrearCliente(Exception falla) =>
        CrearCliente(new RespuestaFija(falla));

    private static AdminApiClient CrearCliente(RespuestaFija manejador)
    {
        var http = new HttpClient(manejador) { BaseAddress = new Uri("http://localhost:5295/") };
        var contexto = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        return new AdminApiClient(http, contexto);
    }

    private sealed class RespuestaFija : HttpMessageHandler
    {
        private readonly HttpResponseMessage? _respuesta;
        private readonly Exception? _falla;

        public RespuestaFija(HttpResponseMessage respuesta) => _respuesta = respuesta;

        public RespuestaFija(Exception falla) => _falla = falla;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _falla is not null ? Task.FromException<HttpResponseMessage>(_falla) : Task.FromResult(_respuesta!);
    }
}
