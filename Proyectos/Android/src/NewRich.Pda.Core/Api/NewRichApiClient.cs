using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NewRich.Application.Contracts.Android;
using NewRich.Application.Contracts.Auth;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Contracts.Premios;
using NewRich.Application.Contracts.Resultados;
using NewRich.Application.Contracts.Ventas;
using NewRich.Constants.Messages;
using NewRich.Shared.Results;

namespace NewRich.Pda.Core.Api;

public interface ITokenStore
{
    Task GuardarAsync(string token);
    Task<string?> ObtenerAsync();
    Task BorrarAsync();
}

public sealed class ApiOpciones
{
    public string BaseUrl { get; set; } = "http://10.0.2.2:5295/";
}

public sealed class NewRichApiClient
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _http;
    private readonly ITokenStore _tokens;
    private readonly ApiOpciones _opciones;

    public NewRichApiClient(HttpClient http, ITokenStore tokens, ApiOpciones opciones)
    {
        _http = http;
        _tokens = tokens;
        _opciones = opciones;
    }

    public async Task<Result> ConectarAsync(IReadOnlyList<string> urls, CancellationToken ct)
    {
        foreach (var url in urls)
        {
            try
            {
                _opciones.BaseUrl = url;
                using var request = Crear(HttpMethod.Get, "swagger/v1/swagger.json", null, null);
                using var response = await _http.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    return Result.Ok(SuccessMessages.OperacionExitosa);
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }
        }

        return Result.Fail(PdaTexts.SinConexionServidor);
    }

    public Task<Result<LoginAndroidResponse>> LoginAsync(LoginRequest request, CancellationToken ct) =>
        EnviarAnonimo<LoginAndroidResponse>(HttpMethod.Post, "api/AuthAndroid/LoginMob", request, ct);

    public Task<Result<LoginAndroidResponse>> CambiarPasswordAsync(CambiarPasswordRequest request, CancellationToken ct) =>
        Enviar<LoginAndroidResponse>(HttpMethod.Post, "api/AuthAndroid/CambiarPasswordMob", request, ct);

    public Task<Result> LogoutAsync(CancellationToken ct) =>
        EnviarSinDatos(HttpMethod.Post, "api/AuthAndroid/LogoutMob", null, ct);

    public Task<Result<IReadOnlyList<LoteriaResponse>>> LoteriasAsync(CancellationToken ct) =>
        Enviar<IReadOnlyList<LoteriaResponse>>(HttpMethod.Get, "api/LoteriasAndroid/ListarMob", null, ct);

    public Task<Result<VentaResponse>> ConfirmarVentaAsync(ConfirmarVentaRequest request, string idempotencyKey, CancellationToken ct) =>
        Enviar<VentaResponse>(HttpMethod.Post, "api/VentasAndroid/ConfirmarMob", request, ct, idempotencyKey);

    public Task<Result<IReadOnlyList<VentaResponse>>> VentasAsync(ConsultaVentasRequest request, CancellationToken ct)
    {
        var q = new List<string>();
        if (request.FechaInicial.HasValue)
        {
            q.Add($"fechaInicial={Uri.EscapeDataString(request.FechaInicial.Value.ToString("o"))}");
        }

        if (request.FechaFinal.HasValue)
        {
            q.Add($"fechaFinal={Uri.EscapeDataString(request.FechaFinal.Value.ToString("o"))}");
        }

        if (!string.IsNullOrWhiteSpace(request.Numero))
        {
            q.Add($"numero={Uri.EscapeDataString(request.Numero)}");
        }

        if (request.LoteriaId.HasValue)
        {
            q.Add($"loteriaId={request.LoteriaId}");
        }

        var url = q.Count == 0 ? "api/VentasAndroid/ConsultarMob" : "api/VentasAndroid/ConsultarMob?" + string.Join("&", q);
        return Enviar<IReadOnlyList<VentaResponse>>(HttpMethod.Get, url, null, ct);
    }

    public Task<Result<IReadOnlyList<ResultadoResponse>>> ResultadosAsync(DateOnly? fecha, Guid? loteriaId, CancellationToken ct)
    {
        var q = new List<string>();
        if (fecha.HasValue)
        {
            q.Add($"fecha={fecha.Value:yyyy-MM-dd}");
        }

        if (loteriaId.HasValue)
        {
            q.Add($"loteriaId={loteriaId}");
        }

        var url = q.Count == 0 ? "api/ResultadosAndroid/ListarMob" : "api/ResultadosAndroid/ListarMob?" + string.Join("&", q);
        return Enviar<IReadOnlyList<ResultadoResponse>>(HttpMethod.Get, url, null, ct);
    }

    public Task<Result<CasoGanadorResponse>> ReportarPremioAsync(ReportarCasoGanadorRequest request, CancellationToken ct) =>
        Enviar<CasoGanadorResponse>(HttpMethod.Post, "api/PremiosAndroid/ReportarMob", request, ct);

    public Task<Result<IReadOnlyList<CasoGanadorResponse>>> PremiosAsync(CancellationToken ct) =>
        Enviar<IReadOnlyList<CasoGanadorResponse>>(HttpMethod.Get, "api/PremiosAndroid/ListarMob", null, ct);

    public Task<Result<ValidacionBoletoResponse>> ValidarQrAsync(ValidarQrRequest request, CancellationToken ct) =>
        Enviar<ValidacionBoletoResponse>(HttpMethod.Post, "api/BoletosAndroid/ValidarQrMob", request, ct);

    public Task<Result<IReadOnlyList<BoletoListaResponse>>> BoletosAsync(FiltroBoletosRequest request, CancellationToken ct)
    {
        var q = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.CodigoPublico))
        {
            q.Add($"codigoPublico={Uri.EscapeDataString(request.CodigoPublico)}");
        }

        if (!string.IsNullOrWhiteSpace(request.Numero))
        {
            q.Add($"numero={Uri.EscapeDataString(request.Numero)}");
        }

        if (request.LoteriaId.HasValue)
        {
            q.Add($"loteriaId={request.LoteriaId}");
        }

        if (!string.IsNullOrWhiteSpace(request.Estado))
        {
            q.Add($"estado={Uri.EscapeDataString(request.Estado)}");
        }

        var url = q.Count == 0 ? "api/BoletosAndroid/FiltrarMob" : "api/BoletosAndroid/FiltrarMob?" + string.Join("&", q);
        return Enviar<IReadOnlyList<BoletoListaResponse>>(HttpMethod.Get, url, null, ct);
    }

    public Task<Result<ConfiguracionOperativaResponse>> OperativaAsync(CancellationToken ct) =>
        Enviar<ConfiguracionOperativaResponse>(HttpMethod.Get, "api/ConfiguracionesAndroid/ObtenerOperativaMob", null, ct);

    public Task<Result<IReadOnlyList<CodigoOfflineAndroidResponse>>> DescargarOfflineAsync(CancellationToken ct) =>
        Enviar<IReadOnlyList<CodigoOfflineAndroidResponse>>(HttpMethod.Post, "api/OfflineAndroid/DescargarMob", new { }, ct);

    public Task<Result<ConversacionResponse>> IniciarChatAsync(IniciarChatRequest request, CancellationToken ct) =>
        Enviar<ConversacionResponse>(HttpMethod.Post, "api/ChatAndroid/IniciarMob", request, ct);

    public Task<Result<IReadOnlyList<ConversacionResponse>>> ChatsAsync(CancellationToken ct) =>
        Enviar<IReadOnlyList<ConversacionResponse>>(HttpMethod.Get, "api/ChatAndroid/ListarMob", null, ct);

    public Task<Result<ConversacionDetalleResponse>> ChatAsync(Guid id, CancellationToken ct) =>
        Enviar<ConversacionDetalleResponse>(HttpMethod.Get, $"api/ChatAndroid/ObtenerMob/{id}", null, ct);

    public Task<Result<MensajeResponse>> EnviarMensajeAsync(Guid id, EnviarMensajeRequest request, CancellationToken ct) =>
        Enviar<MensajeResponse>(HttpMethod.Post, $"api/ChatAndroid/EnviarMob/{id}", request, ct);

    private async Task<Result<T>> EnviarAnonimo<T>(HttpMethod method, string relative, object? body, CancellationToken ct)
    {
        using var request = Crear(method, relative, body, null);
        return await Leer<T>(request, ct);
    }

    private async Task<Result<T>> Enviar<T>(HttpMethod method, string relative, object? body, CancellationToken ct, string? idempotency = null)
    {
        var token = await _tokens.ObtenerAsync();
        using var request = Crear(method, relative, body, token, idempotency);
        return await Leer<T>(request, ct);
    }

    private async Task<Result> EnviarSinDatos(HttpMethod method, string relative, object? body, CancellationToken ct)
    {
        var token = await _tokens.ObtenerAsync();
        using var request = Crear(method, relative, body, token);
        using var response = await _http.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        var envelope = JsonSerializer.Deserialize<ApiEnvelope>(json, Json);
        if (envelope is null)
        {
            return Result.Fail("No se pudo leer la respuesta del servidor.", (int)response.StatusCode);
        }

        return envelope.Success ? Result.Ok(envelope.Message) : Result.Fail(envelope.Message, (int)response.StatusCode);
    }

    private HttpRequestMessage Crear(HttpMethod method, string relative, object? body, string? token, string? idempotency = null)
    {
        var baseUrl = _opciones.BaseUrl.TrimEnd('/') + "/";
        var request = new HttpRequestMessage(method, new Uri(new Uri(baseUrl), relative));
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (!string.IsNullOrWhiteSpace(idempotency))
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotency);
        }

        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");
        }

        return request;
    }

    private async Task<Result<T>> Leer<T>(HttpRequestMessage request, CancellationToken ct)
    {
        using var response = await _http.SendAsync(request, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        var envelope = JsonSerializer.Deserialize<ApiEnvelope>(json, Json);
        if (envelope is null)
        {
            return Result<T>.Fail("No se pudo leer la respuesta del servidor.", (int)response.StatusCode);
        }

        if (!envelope.Success)
        {
            return Result<T>.Fail(envelope.Message, (int)response.StatusCode);
        }

        T? data = default;
        if (envelope.Data.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
        {
            data = envelope.Data.Deserialize<T>(Json);
        }

        return Result<T>.Ok(data!, envelope.Message);
    }

    private sealed class ApiEnvelope
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public JsonElement Data { get; set; }
    }
}
