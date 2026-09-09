using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Application.Contracts.Auth;
using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Chat;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Application.Contracts.Consultas;
using NewRich.Application.Contracts.Dispositivos;
using NewRich.Application.Contracts.Grupos;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Contracts.Notificaciones;
using NewRich.Application.Contracts.Resultados;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Application.Contracts.Ventas;

namespace NewRich.Admin.Services;

public interface IAdminApiClient
{
    Task<ApiCallResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<object>> LogoutAsync(CancellationToken cancellationToken);
    Task<ApiCallResult<LoginResponse>> CambiarPasswordAsync(CambiarPasswordRequest request, CancellationToken cancellationToken);

    Task<ApiCallResult<List<UsuarioResponse>>> ListarUsuariosAsync(CancellationToken cancellationToken);
    Task<ApiCallResult<UsuarioResponse>> ObtenerUsuarioAsync(Guid id, CancellationToken cancellationToken);
    Task<ApiCallResult<UsuarioResponse>> CrearUsuarioAsync(CrearUsuarioRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<UsuarioResponse>> ActualizarUsuarioAsync(Guid id, ActualizarUsuarioRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<object>> EliminarUsuarioAsync(Guid id, CancellationToken cancellationToken);
    Task<ApiCallResult<RestablecerPasswordResponse>> RestablecerPasswordAsync(Guid id, CancellationToken cancellationToken);
    Task<ApiCallResult<UsuarioResponse>> DesbloquearUsuarioAsync(Guid id, CancellationToken cancellationToken);

    Task<ApiCallResult<List<DispositivoResponse>>> ListarDispositivosAsync(CancellationToken cancellationToken);
    Task<ApiCallResult<DispositivoResponse>> CrearDispositivoAsync(CrearDispositivoRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<DispositivoResponse>> ActualizarDispositivoAsync(Guid id, ActualizarDispositivoRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<DispositivoResponse>> AsociarDispositivoAsync(Guid dispositivoId, Guid usuarioId, CancellationToken cancellationToken);
    Task<ApiCallResult<DispositivoResponse>> DesasociarDispositivoAsync(Guid dispositivoId, Guid usuarioId, CancellationToken cancellationToken);
    Task<ApiCallResult<object>> DesasociarDispositivoAsync(Guid dispositivoId, CancellationToken cancellationToken);
    Task<ApiCallResult<object>> EliminarDispositivoAsync(Guid dispositivoId, CancellationToken cancellationToken);

    Task<ApiCallResult<List<LoteriaResponse>>> ListarLoteriasAsync(CancellationToken cancellationToken);
    Task<ApiCallResult<LoteriaResponse>> CrearLoteriaAsync(CrearLoteriaRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<LoteriaResponse>> ActualizarLoteriaAsync(Guid id, ActualizarLoteriaRequest request, CancellationToken cancellationToken);

    Task<ApiCallResult<List<GrupoResponse>>> ListarGruposAsync(CancellationToken cancellationToken);
    Task<ApiCallResult<GrupoResponse>> ObtenerGrupoAsync(Guid id, CancellationToken cancellationToken);
    Task<ApiCallResult<GrupoResponse>> CrearGrupoAsync(CrearGrupoRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<GrupoResponse>> ActualizarGrupoAsync(Guid id, ActualizarGrupoRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<object>> EliminarGrupoAsync(Guid id, CancellationToken cancellationToken);
    Task<ApiCallResult<object>> AsignarVendedorGrupoAsync(Guid grupoId, Guid usuarioId, CancellationToken cancellationToken);
    Task<ApiCallResult<object>> DesasignarVendedorGrupoAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task<ApiCallResult<List<ResultadoResponse>>> ListarResultadosAsync(DateOnly? fecha, Guid? loteriaId, CancellationToken cancellationToken);
    Task<ApiCallResult<ResultadoResponse>> RegistrarResultadoAsync(RegistrarResultadoRequest request, CancellationToken cancellationToken);

    Task<ApiCallResult<List<VentaResponse>>> ConsultarVentasAsync(ConsultaVentasRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<List<BoletoListaResponse>>> FiltrarBoletosAsync(FiltroBoletosRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<TirillaResponse>> ObtenerTirillaAsync(Guid boletoId, CancellationToken cancellationToken);
    Task<ApiCallResult<object>> AutorizarPagoAsync(Guid boletoId, CancellationToken cancellationToken);
    Task<ApiCallResult<List<BusquedaAdministrativaResponse>>> BuscarConsultasAsync(BusquedaAdministrativaRequest request, CancellationToken cancellationToken);

    Task<ApiCallResult<KpiResponse>> ConsultarKpiAsync(KpiRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<NotificacionesResponse>> ListarNotificacionesAsync(CancellationToken cancellationToken);
    Task<ApiCallResult<object>> MarcarNotificacionesLeidasAsync(CancellationToken cancellationToken);

    Task<ApiCallResult<List<ConversacionResponse>>> ListarConversacionesAsync(CancellationToken cancellationToken);
    Task<ApiCallResult<ConversacionDetalleResponse>> ObtenerConversacionAsync(Guid id, CancellationToken cancellationToken);
    Task<ApiCallResult<ConversacionResponse>> IniciarConversacionAsync(IniciarChatRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<MensajeResponse>> EnviarMensajeAsync(Guid conversacionId, EnviarMensajeRequest request, CancellationToken cancellationToken);
    Task<ApiCallResult<object>> CerrarConversacionAsync(Guid id, CancellationToken cancellationToken);

    Task<ApiCallResult<List<ConfiguracionResponse>>> ListarConfiguracionesAsync(CancellationToken cancellationToken);
    Task<ApiCallResult<ConfiguracionResponse>> ActualizarConfiguracionAsync(string clave, string valor, CancellationToken cancellationToken);
}

public sealed class AdminApiClient : IAdminApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminApiClient(HttpClient http, IHttpContextAccessor httpContextAccessor)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<ApiCallResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
        SendAsync<LoginResponse>(HttpMethod.Post, "api/Auth/login", request, includeToken: false, cancellationToken);

    public Task<ApiCallResult<object>> LogoutAsync(CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, "api/Auth/logout", null, includeToken: true, cancellationToken);

    public Task<ApiCallResult<LoginResponse>> CambiarPasswordAsync(CambiarPasswordRequest request, CancellationToken cancellationToken) =>
        SendAsync<LoginResponse>(HttpMethod.Post, "api/Auth/cambiar-password", request, includeToken: true, cancellationToken);

    public Task<ApiCallResult<List<UsuarioResponse>>> ListarUsuariosAsync(CancellationToken cancellationToken) =>
        SendAsync<List<UsuarioResponse>>(HttpMethod.Get, "api/Usuarios", null, true, cancellationToken);

    public Task<ApiCallResult<UsuarioResponse>> ObtenerUsuarioAsync(Guid id, CancellationToken cancellationToken) =>
        SendAsync<UsuarioResponse>(HttpMethod.Get, $"api/Usuarios/{id}", null, true, cancellationToken);

    public Task<ApiCallResult<UsuarioResponse>> CrearUsuarioAsync(CrearUsuarioRequest request, CancellationToken cancellationToken) =>
        SendAsync<UsuarioResponse>(HttpMethod.Post, "api/Usuarios", request, true, cancellationToken);

    public Task<ApiCallResult<UsuarioResponse>> ActualizarUsuarioAsync(Guid id, ActualizarUsuarioRequest request, CancellationToken cancellationToken) =>
        SendAsync<UsuarioResponse>(HttpMethod.Put, $"api/Usuarios/{id}", request, true, cancellationToken);

    public Task<ApiCallResult<object>> EliminarUsuarioAsync(Guid id, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Delete, $"api/Usuarios/{id}", null, true, cancellationToken);

    public Task<ApiCallResult<RestablecerPasswordResponse>> RestablecerPasswordAsync(Guid id, CancellationToken cancellationToken) =>
        SendAsync<RestablecerPasswordResponse>(HttpMethod.Post, $"api/Usuarios/{id}/restablecer-password", null, true, cancellationToken);

    public Task<ApiCallResult<UsuarioResponse>> DesbloquearUsuarioAsync(Guid id, CancellationToken cancellationToken) =>
        SendAsync<UsuarioResponse>(HttpMethod.Post, $"api/Usuarios/{id}/desbloquear", null, true, cancellationToken);

    public Task<ApiCallResult<List<DispositivoResponse>>> ListarDispositivosAsync(CancellationToken cancellationToken) =>
        SendAsync<List<DispositivoResponse>>(HttpMethod.Get, "api/Dispositivos", null, true, cancellationToken);

    public Task<ApiCallResult<DispositivoResponse>> CrearDispositivoAsync(CrearDispositivoRequest request, CancellationToken cancellationToken) =>
        SendAsync<DispositivoResponse>(HttpMethod.Post, "api/Dispositivos", request, true, cancellationToken);

    public Task<ApiCallResult<DispositivoResponse>> ActualizarDispositivoAsync(Guid id, ActualizarDispositivoRequest request, CancellationToken cancellationToken) =>
        SendAsync<DispositivoResponse>(HttpMethod.Put, $"api/Dispositivos/{id}", request, true, cancellationToken);

    public Task<ApiCallResult<DispositivoResponse>> AsociarDispositivoAsync(Guid dispositivoId, Guid usuarioId, CancellationToken cancellationToken) =>
        SendAsync<DispositivoResponse>(HttpMethod.Post, $"api/Dispositivos/{dispositivoId}/asociar/{usuarioId}", null, true, cancellationToken);

    public Task<ApiCallResult<DispositivoResponse>> DesasociarDispositivoAsync(Guid dispositivoId, Guid usuarioId, CancellationToken cancellationToken) =>
        SendAsync<DispositivoResponse>(HttpMethod.Post, $"api/Dispositivos/{dispositivoId}/desasociar/{usuarioId}", null, true, cancellationToken);

    public Task<ApiCallResult<object>> DesasociarDispositivoAsync(Guid dispositivoId, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, $"api/Dispositivos/{dispositivoId}/desasociar", null, true, cancellationToken);

    public Task<ApiCallResult<object>> EliminarDispositivoAsync(Guid dispositivoId, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Delete, $"api/Dispositivos/{dispositivoId}", null, true, cancellationToken);

    public Task<ApiCallResult<List<LoteriaResponse>>> ListarLoteriasAsync(CancellationToken cancellationToken) =>
        SendAsync<List<LoteriaResponse>>(HttpMethod.Get, "api/Loterias", null, true, cancellationToken);

    public Task<ApiCallResult<LoteriaResponse>> CrearLoteriaAsync(CrearLoteriaRequest request, CancellationToken cancellationToken) =>
        SendAsync<LoteriaResponse>(HttpMethod.Post, "api/Loterias", request, true, cancellationToken);

    public Task<ApiCallResult<LoteriaResponse>> ActualizarLoteriaAsync(Guid id, ActualizarLoteriaRequest request, CancellationToken cancellationToken) =>
        SendAsync<LoteriaResponse>(HttpMethod.Put, $"api/Loterias/{id}", request, true, cancellationToken);

    public Task<ApiCallResult<List<GrupoResponse>>> ListarGruposAsync(CancellationToken cancellationToken) =>
        SendAsync<List<GrupoResponse>>(HttpMethod.Get, "api/Grupos", null, true, cancellationToken);

    public Task<ApiCallResult<GrupoResponse>> ObtenerGrupoAsync(Guid id, CancellationToken cancellationToken) =>
        SendAsync<GrupoResponse>(HttpMethod.Get, $"api/Grupos/{id}", null, true, cancellationToken);

    public Task<ApiCallResult<GrupoResponse>> CrearGrupoAsync(CrearGrupoRequest request, CancellationToken cancellationToken) =>
        SendAsync<GrupoResponse>(HttpMethod.Post, "api/Grupos", request, true, cancellationToken);

    public Task<ApiCallResult<GrupoResponse>> ActualizarGrupoAsync(Guid id, ActualizarGrupoRequest request, CancellationToken cancellationToken) =>
        SendAsync<GrupoResponse>(HttpMethod.Put, $"api/Grupos/{id}", request, true, cancellationToken);

    public Task<ApiCallResult<object>> EliminarGrupoAsync(Guid id, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Delete, $"api/Grupos/{id}", null, true, cancellationToken);

    public Task<ApiCallResult<object>> AsignarVendedorGrupoAsync(Guid grupoId, Guid usuarioId, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, $"api/Grupos/{grupoId}/asignar/{usuarioId}", null, true, cancellationToken);

    public Task<ApiCallResult<object>> DesasignarVendedorGrupoAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, $"api/Grupos/desasignar/{usuarioId}", null, true, cancellationToken);

    public Task<ApiCallResult<List<ResultadoResponse>>> ListarResultadosAsync(DateOnly? fecha, Guid? loteriaId, CancellationToken cancellationToken)
    {
        var query = new List<string>();
        if (fecha.HasValue)
        {
            query.Add($"fecha={fecha.Value:yyyy-MM-dd}");
        }

        if (loteriaId.HasValue)
        {
            query.Add($"loteriaId={loteriaId}");
        }

        var path = query.Count == 0 ? "api/Resultados" : "api/Resultados?" + string.Join("&", query);
        return SendAsync<List<ResultadoResponse>>(HttpMethod.Get, path, null, true, cancellationToken);
    }

    public Task<ApiCallResult<ResultadoResponse>> RegistrarResultadoAsync(RegistrarResultadoRequest request, CancellationToken cancellationToken) =>
        SendAsync<ResultadoResponse>(HttpMethod.Post, "api/Resultados", request, true, cancellationToken);

    public Task<ApiCallResult<List<VentaResponse>>> ConsultarVentasAsync(ConsultaVentasRequest request, CancellationToken cancellationToken)
    {
        var q = BuildQuery(
            ("vendedorId", request.VendedorId?.ToString()),
            ("fechaInicial", request.FechaInicial?.ToString("o")),
            ("fechaFinal", request.FechaFinal?.ToString("o")),
            ("numero", request.Numero),
            ("loteriaId", request.LoteriaId?.ToString()));
        return SendAsync<List<VentaResponse>>(HttpMethod.Get, "api/Ventas" + q, null, true, cancellationToken);
    }

    public Task<ApiCallResult<List<BoletoListaResponse>>> FiltrarBoletosAsync(FiltroBoletosRequest request, CancellationToken cancellationToken)
    {
        var q = BuildQuery(
            ("estado", request.Estado),
            ("vendedorId", request.VendedorId?.ToString()),
            ("codigoPublico", request.CodigoPublico),
            ("numero", request.Numero),
            ("loteriaId", request.LoteriaId?.ToString()),
            ("fechaInicial", request.FechaInicial?.ToString("o")),
            ("fechaFinal", request.FechaFinal?.ToString("o")));
        return SendAsync<List<BoletoListaResponse>>(HttpMethod.Get, "api/Boletos" + q, null, true, cancellationToken);
    }

    public Task<ApiCallResult<TirillaResponse>> ObtenerTirillaAsync(Guid boletoId, CancellationToken cancellationToken) =>
        SendAsync<TirillaResponse>(HttpMethod.Get, $"api/Boletos/{boletoId}/tirilla", null, true, cancellationToken);

    public Task<ApiCallResult<object>> AutorizarPagoAsync(Guid boletoId, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, $"api/Boletos/{boletoId}/pagar", null, true, cancellationToken);

    public Task<ApiCallResult<List<BusquedaAdministrativaResponse>>> BuscarConsultasAsync(BusquedaAdministrativaRequest request, CancellationToken cancellationToken)
    {
        var q = BuildQuery(
            ("numero", request.Numero),
            ("vendedor", request.Vendedor),
            ("codigoBoleto", request.CodigoBoleto),
            ("loteriaId", request.LoteriaId?.ToString()),
            ("fecha", request.Fecha?.ToString("o")),
            ("estado", request.Estado));
        return SendAsync<List<BusquedaAdministrativaResponse>>(HttpMethod.Get, "api/Consultas" + q, null, true, cancellationToken);
    }

    public Task<ApiCallResult<KpiResponse>> ConsultarKpiAsync(KpiRequest request, CancellationToken cancellationToken)
    {
        var q = BuildQuery(
            ("vendedorId", request.VendedorId?.ToString()),
            ("fechaInicial", request.FechaInicial?.ToString("o")),
            ("fechaFinal", request.FechaFinal?.ToString("o")));
        return SendAsync<KpiResponse>(HttpMethod.Get, "api/Kpi" + q, null, true, cancellationToken);
    }

    public Task<ApiCallResult<NotificacionesResponse>> ListarNotificacionesAsync(CancellationToken cancellationToken) =>
        SendAsync<NotificacionesResponse>(HttpMethod.Get, "api/Notificaciones", null, true, cancellationToken);

    public Task<ApiCallResult<object>> MarcarNotificacionesLeidasAsync(CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, "api/Notificaciones/marcar-leidas", null, true, cancellationToken);

    public Task<ApiCallResult<List<ConversacionResponse>>> ListarConversacionesAsync(CancellationToken cancellationToken) =>
        SendAsync<List<ConversacionResponse>>(HttpMethod.Get, "api/Chat", null, true, cancellationToken);

    public Task<ApiCallResult<ConversacionDetalleResponse>> ObtenerConversacionAsync(Guid id, CancellationToken cancellationToken) =>
        SendAsync<ConversacionDetalleResponse>(HttpMethod.Get, $"api/Chat/{id}", null, true, cancellationToken);

    public Task<ApiCallResult<ConversacionResponse>> IniciarConversacionAsync(IniciarChatRequest request, CancellationToken cancellationToken) =>
        SendAsync<ConversacionResponse>(HttpMethod.Post, "api/Chat", request, true, cancellationToken);

    public Task<ApiCallResult<MensajeResponse>> EnviarMensajeAsync(Guid conversacionId, EnviarMensajeRequest request, CancellationToken cancellationToken) =>
        SendAsync<MensajeResponse>(HttpMethod.Post, $"api/Chat/{conversacionId}/mensajes", request, true, cancellationToken);

    public Task<ApiCallResult<object>> CerrarConversacionAsync(Guid id, CancellationToken cancellationToken) =>
        SendAsync<object>(HttpMethod.Post, $"api/Chat/{id}/cerrar", null, true, cancellationToken);

    public Task<ApiCallResult<List<ConfiguracionResponse>>> ListarConfiguracionesAsync(CancellationToken cancellationToken) =>
        SendAsync<List<ConfiguracionResponse>>(HttpMethod.Get, "api/Configuraciones", null, true, cancellationToken);

    public Task<ApiCallResult<ConfiguracionResponse>> ActualizarConfiguracionAsync(string clave, string valor, CancellationToken cancellationToken) =>
        SendAsync<ConfiguracionResponse>(HttpMethod.Put, $"api/Configuraciones/{Uri.EscapeDataString(clave)}", new ActualizarConfiguracionRequest { Valor = valor }, true, cancellationToken);

    private async Task<ApiCallResult<T>> SendAsync<T>(HttpMethod method, string path, object? body, bool includeToken, CancellationToken cancellationToken)
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

            ApiEnvelope<T>? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<ApiEnvelope<T>>(json, JsonOptions);
            }
            catch (JsonException)
            {
                return ApiCallResult<T>.Fail(
                    $"La API respondió HTTP {(int)response.StatusCode} sin un contrato válido.",
                    (int)response.StatusCode,
                    response.StatusCode == HttpStatusCode.Unauthorized);
            }

            if (envelope is null)
            {
                return ApiCallResult<T>.Fail(
                    $"La API respondió HTTP {(int)response.StatusCode} sin un contrato válido.",
                    (int)response.StatusCode,
                    response.StatusCode == HttpStatusCode.Unauthorized);
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

    private static string BuildQuery(params (string Key, string? Value)[] pairs)
    {
        var items = pairs
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}")
            .ToList();
        return items.Count == 0 ? string.Empty : "?" + string.Join("&", items);
    }
}
