using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NewRich.Application.Contracts.Auth;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Application.Contracts.JuegoNumerosManual;
using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Contracts.Notificaciones;
using NewRich.Application.Contracts.Premios;
using NewRich.Application.Contracts.RegistrarJuegoManual;
using NewRich.Application.Contracts.RegistrarJuegoNumerosWebManual;
using NewRich.Application.Contracts.Resultados;
using NewRich.Application.Contracts.Ventas;
using NewRich.Vendedor.Constants;
using NewRich.Vendedor.Models;

namespace NewRich.Vendedor.Services;

public interface IVendedorApiClient
{
    Task<ApiCallResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<object>> LogoutAsync(CancellationToken cancellationToken);
    Task<ApiCallResult<LoginResponse>> CambiarPasswordAsync(CambiarPasswordRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<ConfiguracionOperativaResponse>> OperativaAsync(CancellationToken cancellationToken);
    Task<ApiCallResult<List<LoteriaResponse>>> LoteriasAsync(CancellationToken cancellationToken);
    Task<ApiCallResult<List<LoteriaResponse>>> LoteriasTodasAsync(CancellationToken cancellationToken);
    Task<ApiCallResult<RegistroJuegoManualResponse>> RegistrarJuegoAsync(RegistrarJuegoManualRequest request, string idempotencyKey, CancellationToken cancellationToken);
    Task<ApiCallResult<List<FilaHistoricoJuegoManualResponse>>> ConsultarRegistrosAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken);
    Task<ApiCallResult<VentaResponse>> ConfirmarAsync(ConfirmarVentaRequest request, string idempotencyKey, CancellationToken cancellationToken);
    Task<ApiCallResult<VentaResponse>> RegistrarNumerosAsync(RegistrarJuegoNumerosManualRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<RegistroJuegoNumerosWebManualResponse>> RegistrarJuegoNumerosWebAsync(RegistrarJuegoNumerosWebManualRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<List<VentaResponse>>> ConsultarVentasAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken);
    Task<ApiCallResult<NotificacionesResponse>> AlertasAsync(CancellationToken cancellationToken);
    Task<ApiCallResult<object>> MarcarAlertasLeidasAsync(CancellationToken cancellationToken);
    Task<ApiCallResult<ConsultaTicketResponse>> ConsultarTicketAsync(string codigo, CancellationToken cancellationToken);
    Task<ApiCallResult<List<ResultadoResponse>>> ResultadosAsync(DateOnly? fecha, Guid? loteriaId, CancellationToken cancellationToken);
    Task<ApiCallResult<List<ConversacionResponse>>> ConversacionesAsync(bool tecnico, CancellationToken cancellationToken);
    Task<ApiCallResult<ConversacionResponse>> IniciarConversacionAsync(string texto, CancellationToken cancellationToken);
    Task<ApiCallResult<ConversacionDetalleResponse>> ConversacionAsync(Guid id, CancellationToken cancellationToken);
    Task<ApiCallResult<MensajeResponse>> EnviarMensajeAsync(Guid id, string texto, CancellationToken cancellationToken);
    Task<ApiCallResult<object>> ReportarTecnicoAsync(string observacion, string codigoTicket, CancellationToken cancellationToken);
    Task<ApiCallResult<List<CasoGanadorResponse>>> EntregasAsync(CancellationToken cancellationToken);
    Task<ApiCallResult<CasoGanadorResponse>> RegistrarEntregaAsync(Guid casoId, decimal valorGanado, CancellationToken cancellationToken);
}

public sealed class VendedorApiClient : IVendedorApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public VendedorApiClient(HttpClient http, IHttpContextAccessor httpContextAccessor)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<ApiCallResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
        SendAsync<LoginResponse>(HttpMethod.Post, "api/AuthManual/login", request, false, null, cancellationToken);

    public Task<ApiCallResult<object>> LogoutAsync(CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, "api/Auth/logout", null, true, null, cancellationToken);

    public Task<ApiCallResult<LoginResponse>> CambiarPasswordAsync(CambiarPasswordRequest request, CancellationToken cancellationToken) =>
        SendAsync<LoginResponse>(HttpMethod.Post, "api/Auth/cambiar-password", request, true, null, cancellationToken);

    public Task<ApiCallResult<ConfiguracionOperativaResponse>> OperativaAsync(CancellationToken cancellationToken) =>
        SendAsync<ConfiguracionOperativaResponse>(HttpMethod.Get, "api/ConfiguracionesAndroid/ObtenerOperativaMob", null, true, null, cancellationToken);

    public Task<ApiCallResult<List<LoteriaResponse>>> LoteriasAsync(CancellationToken cancellationToken) =>
        SendAsync<List<LoteriaResponse>>(HttpMethod.Get, "api/LoteriasAndroid/ListarMob", null, true, null, cancellationToken);

    public Task<ApiCallResult<List<LoteriaResponse>>> LoteriasTodasAsync(CancellationToken cancellationToken) =>
        SendAsync<List<LoteriaResponse>>(HttpMethod.Get, "api/RegistrarJuegoManual/Loterias", null, true, null, cancellationToken);

    public Task<ApiCallResult<RegistroJuegoManualResponse>> RegistrarJuegoAsync(RegistrarJuegoManualRequest request, string idempotencyKey, CancellationToken cancellationToken) =>
        SendAsync<RegistroJuegoManualResponse>(HttpMethod.Post, "api/RegistrarJuegoManual/Registrar", request, true, idempotencyKey, cancellationToken);

    public Task<ApiCallResult<List<FilaHistoricoJuegoManualResponse>>> ConsultarRegistrosAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken)
    {
        var path = "api/RegistrarJuegoManual/Consultar?fechaInicial="
            + Uri.EscapeDataString(desde.ToString("yyyy-MM-dd"))
            + "&fechaFinal="
            + Uri.EscapeDataString(hasta.Date.ToString("yyyy-MM-dd") + "T23:59:59");
        return SendAsync<List<FilaHistoricoJuegoManualResponse>>(HttpMethod.Get, path, null, true, null, cancellationToken);
    }

    public Task<ApiCallResult<VentaResponse>> ConfirmarAsync(ConfirmarVentaRequest request, string idempotencyKey, CancellationToken cancellationToken) =>
        SendAsync<VentaResponse>(HttpMethod.Post, "api/VentasManual/Confirmar", request, true, idempotencyKey, cancellationToken);

    public Task<ApiCallResult<VentaResponse>> RegistrarNumerosAsync(RegistrarJuegoNumerosManualRequest request, CancellationToken cancellationToken) =>
        SendAsync<VentaResponse>(HttpMethod.Post, "api/VentasManual/RegistrarNumeros", request, true, null, cancellationToken);

    public Task<ApiCallResult<RegistroJuegoNumerosWebManualResponse>> RegistrarJuegoNumerosWebAsync(RegistrarJuegoNumerosWebManualRequest request, CancellationToken cancellationToken) =>
        SendAsync<RegistroJuegoNumerosWebManualResponse>(HttpMethod.Post, "api/RegistrarJuegoNumerosWebManual/Registrar", request, true, null, cancellationToken);

    public Task<ApiCallResult<List<VentaResponse>>> ConsultarVentasAsync(DateTime desde, DateTime hasta, CancellationToken cancellationToken)
    {
        var path = "api/VentasManual/Consultar?fechaInicial="
            + Uri.EscapeDataString(desde.ToString("yyyy-MM-dd"))
            + "&fechaFinal="
            + Uri.EscapeDataString(hasta.Date.ToString("yyyy-MM-dd") + "T23:59:59");
        return SendAsync<List<VentaResponse>>(HttpMethod.Get, path, null, true, null, cancellationToken);
    }

    public Task<ApiCallResult<NotificacionesResponse>> AlertasAsync(CancellationToken cancellationToken) =>
        SendAsync<NotificacionesResponse>(HttpMethod.Get, "api/Notificaciones", null, true, null, cancellationToken);

    public Task<ApiCallResult<object>> MarcarAlertasLeidasAsync(CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, "api/Notificaciones/marcar-leidas", null, true, null, cancellationToken);

    public Task<ApiCallResult<ConsultaTicketResponse>> ConsultarTicketAsync(string codigo, CancellationToken cancellationToken) =>
        SendAsync<ConsultaTicketResponse>(HttpMethod.Post, "api/VentasManual/ConsultarCodigo", new ConsultaTicketRequest { TicketCode = codigo }, true, null, cancellationToken);

    public Task<ApiCallResult<List<ResultadoResponse>>> ResultadosAsync(DateOnly? fecha, Guid? loteriaId, CancellationToken cancellationToken)
    {
        var partes = new List<string>();
        if (fecha.HasValue)
        {
            partes.Add("fecha=" + fecha.Value.ToString("yyyy-MM-dd"));
        }

        if (loteriaId.HasValue)
        {
            partes.Add("loteriaId=" + loteriaId.Value);
        }

        var path = partes.Count == 0
            ? "api/ResultadosAndroid/ListarMob"
            : "api/ResultadosAndroid/ListarMob?" + string.Join("&", partes);
        return SendAsync<List<ResultadoResponse>>(HttpMethod.Get, path, null, true, null, cancellationToken);
    }

    public Task<ApiCallResult<List<ConversacionResponse>>> ConversacionesAsync(bool tecnico, CancellationToken cancellationToken) =>
        SendAsync<List<ConversacionResponse>>(
            HttpMethod.Get,
            tecnico ? "api/ChatAndroid/ListarTecnicoMob" : "api/ChatAndroid/ListarMob",
            null,
            true,
            null,
            cancellationToken);

    public Task<ApiCallResult<ConversacionResponse>> IniciarConversacionAsync(string texto, CancellationToken cancellationToken) =>
        SendAsync<ConversacionResponse>(HttpMethod.Post, "api/ChatAndroid/IniciarMob", new IniciarChatRequest { Texto = texto }, true, null, cancellationToken);

    public Task<ApiCallResult<ConversacionDetalleResponse>> ConversacionAsync(Guid id, CancellationToken cancellationToken) =>
        SendAsync<ConversacionDetalleResponse>(HttpMethod.Get, $"api/ChatAndroid/ObtenerMob/{id}", null, true, null, cancellationToken);

    public Task<ApiCallResult<MensajeResponse>> EnviarMensajeAsync(Guid id, string texto, CancellationToken cancellationToken) =>
        SendAsync<MensajeResponse>(HttpMethod.Post, $"api/ChatAndroid/EnviarMob/{id}", new EnviarMensajeRequest { Texto = texto }, true, null, cancellationToken);

    public Task<ApiCallResult<object>> ReportarTecnicoAsync(string observacion, string codigoTicket, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, "api/ChatAndroid/ReporteTecnicoMob", new ReporteTecnicoRequest
        {
            Observacion = observacion,
            CodigoTicket = codigoTicket
        }, true, null, cancellationToken);

    public Task<ApiCallResult<List<CasoGanadorResponse>>> EntregasAsync(CancellationToken cancellationToken) =>
        SendAsync<List<CasoGanadorResponse>>(HttpMethod.Get, "api/PremiosManual/Listar", null, true, null, cancellationToken);

    public Task<ApiCallResult<CasoGanadorResponse>> RegistrarEntregaAsync(Guid casoId, decimal valorGanado, CancellationToken cancellationToken) =>
        SendAsync<CasoGanadorResponse>(
            HttpMethod.Post,
            $"api/PremiosManual/RegistrarEntrega/{casoId}",
            new RegistrarEntregaVendedorRequest { ValorGanado = valorGanado },
            true,
            null,
            cancellationToken);

    private async Task<ApiCallResult<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        bool includeToken,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (includeToken)
            {
                var token = _httpContextAccessor.HttpContext?.Request.Cookies[AuthCookieNames.AccessToken];
                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }
            }

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
            }

            if (body is not null)
            {
                request.Content = JsonContent.Create(body);
            }

            using var response = await _http.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(json))
            {
                return ApiCallResult<T>.Fail(UiTexts.ApiNoDisponible, (int)response.StatusCode, response.StatusCode == HttpStatusCode.Unauthorized);
            }

            var envelope = JsonSerializer.Deserialize<ApiEnvelope<T>>(json, JsonOptions);
            if (envelope is null)
            {
                return ApiCallResult<T>.Fail(UiTexts.ApiNoDisponible, (int)response.StatusCode);
            }

            if (!envelope.Success)
            {
                return ApiCallResult<T>.Fail(
                    string.IsNullOrWhiteSpace(envelope.Message) ? UiTexts.ApiNoDisponible : envelope.Message,
                    (int)response.StatusCode,
                    response.StatusCode == HttpStatusCode.Unauthorized);
            }

            return ApiCallResult<T>.Ok(envelope.Data, envelope.Message, (int)response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return ApiCallResult<T>.Fail(UiTexts.ApiNoDisponible, 0);
        }
        catch (TaskCanceledException)
        {
            return ApiCallResult<T>.Fail(UiTexts.ApiNoDisponible, 0);
        }
    }
}
