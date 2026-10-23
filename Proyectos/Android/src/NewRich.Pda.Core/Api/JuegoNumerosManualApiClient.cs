using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NewRich.Application.Contracts.JuegoNumerosManual;
using NewRich.Shared.Results;

namespace NewRich.Pda.Core.Api;

/// <summary>
/// Cliente de los endpoints nuevos de «Combo» (api/JuegoNumerosManual).
/// Un fallo de red se devuelve con código 0 para distinguirlo de un rechazo del servidor.
/// </summary>
public sealed class JuegoNumerosManualApiClient
{
    public const int SinRed = 0;

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _http;
    private readonly ITokenStore _tokens;
    private readonly ApiOpciones _opciones;

    public JuegoNumerosManualApiClient(HttpClient http, ITokenStore tokens, ApiOpciones opciones)
    {
        _http = http;
        _tokens = tokens;
        _opciones = opciones;
    }

    public Task<Result<JuegoNumerosManualResponse>> RegistrarAsync(RegistrarJuegoNumerosManualRequest request, CancellationToken ct) =>
        EnviarAsync("api/JuegoNumerosManual/Registrar", request, ct);

    /// <summary>Sin red, tiempo agotado o servidor caído. Reintentar con el mismo identificador no duplica la jugada.</summary>
    public static bool EsFalloDeConexion(int estado) =>
        estado == SinRed || estado == 408 || estado == 429 || estado >= 500;

    private async Task<Result<JuegoNumerosManualResponse>> EnviarAsync(string ruta, object cuerpo, CancellationToken ct)
    {
        var token = await _tokens.ObtenerAsync();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(_opciones.BaseUrl.TrimEnd('/') + "/"), ruta))
            {
                Content = new StringContent(JsonSerializer.Serialize(cuerpo, Json), Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await _http.SendAsync(request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            return Leer(json, (int)response.StatusCode);
        }
        catch (Exception)
        {
            return Result<JuegoNumerosManualResponse>.Fail(PdaTexts.SinConexionServidor, SinRed);
        }
    }

    public static Result<JuegoNumerosManualResponse> Leer(string json, int estado)
    {
        Sobre? sobre = null;
        try
        {
            sobre = JsonSerializer.Deserialize<Sobre>(json, Json);
        }
        catch (JsonException)
        {
        }

        if (sobre is null)
        {
            return Result<JuegoNumerosManualResponse>.Fail(PdaTexts.SinConexionServidor, estado >= 500 ? estado : SinRed);
        }

        if (!sobre.Success || sobre.Data is null)
        {
            return Result<JuegoNumerosManualResponse>.Fail(sobre.Message, estado);
        }

        return Result<JuegoNumerosManualResponse>.Ok(sobre.Data, sobre.Message);
    }

    private sealed class Sobre
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public JuegoNumerosManualResponse? Data { get; set; }
    }
}
