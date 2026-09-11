using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
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
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IAndroidPdaService
{
    Task<Result<LoginAndroidResponse>> LoginMobAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<Result<LoginAndroidResponse>> CambiarPasswordMobAsync(Guid usuarioId, Guid sesionId, CambiarPasswordRequest request, CancellationToken cancellationToken);
    Task<Result> LogoutMobAsync(Guid sesionId, CancellationToken cancellationToken);
    Task<Result<LoginAndroidResponse>> PerfilMobAsync(Guid usuarioId, Guid? dispositivoId, CancellationToken cancellationToken);
    Task<Result<ConfiguracionOperativaResponse>> ObtenerOperativaMobAsync(CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<LoteriaResponse>>> LoteriasMobAsync(CancellationToken cancellationToken);
    Task<Result<VentaResponse>> ConfirmarVentaMobAsync(Guid vendedorId, Guid? dispositivoId, ConfirmarVentaRequest request, string? idempotencyKey, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<VentaResponse>>> ConsultarVentasMobAsync(ConsultaVentasRequest request, Guid solicitanteId, bool soloPropias, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ResultadoResponse>>> ResultadosMobAsync(DateOnly? fecha, Guid? loteriaId, CancellationToken cancellationToken);
    Task<Result<CasoGanadorResponse>> ReportarPremioMobAsync(Guid solicitanteId, ReportarCasoGanadorRequest request, CancellationToken cancellationToken);
    Task<Result<DescargaAdjuntoResponse>> ObtenerFotoPremioMobAsync(Guid casoId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<CasoGanadorResponse>>> PremiosMobAsync(CancellationToken cancellationToken);
    Task<Result<ValidacionBoletoResponse>> ValidarQrMobAsync(ValidarQrRequest request, CancellationToken cancellationToken);
    Task<Result<ConsultaTicketResponse>> ConsultarTicketMobAsync(string? ticketCode, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<BoletoListaResponse>>> FiltrarBoletosMobAsync(FiltroBoletosRequest request, CancellationToken cancellationToken);
    Task<Result<TirillaResponse>> TirillaMobAsync(Guid boletoId, CancellationToken cancellationToken);
    Task<Result<ConversacionResponse>> IniciarChatMobAsync(Guid iniciadorId, IniciarChatRequest request, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ConversacionResponse>>> ListarChatMobAsync(Guid usuarioId, CancellationToken cancellationToken);
    Task<Result<ConversacionDetalleResponse>> ObtenerChatMobAsync(Guid conversacionId, Guid usuarioId, CancellationToken cancellationToken);
    Task<Result<MensajeResponse>> EnviarChatMobAsync(Guid conversacionId, Guid emisorId, EnviarMensajeRequest request, CancellationToken cancellationToken);
    Task<Result<DescargaAdjuntoResponse>> DescargarAdjuntoMobAsync(Guid adjuntoId, Guid usuarioId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<CodigoOfflineAndroidResponse>>> DescargarOfflineMobAsync(Guid usuarioId, Guid? dispositivoId, CancellationToken cancellationToken);
}

public sealed class AndroidPdaService : IAndroidPdaService
{
    private readonly IAuthService _auth;
    private readonly IVentaService _ventas;
    private readonly ILoteriaService _loterias;
    private readonly IResultadoService _resultados;
    private readonly IPremioService _premios;
    private readonly IValidacionBoletoService _boletos;
    private readonly IChatService _chat;
    private readonly IConfiguracionService _configuracion;
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public AndroidPdaService(
        IAuthService auth,
        IVentaService ventas,
        ILoteriaService loterias,
        IResultadoService resultados,
        IPremioService premios,
        IValidacionBoletoService boletos,
        IChatService chat,
        IConfiguracionService configuracion,
        INewRichDbContext db,
        IClock clock)
    {
        _auth = auth;
        _ventas = ventas;
        _loterias = loterias;
        _resultados = resultados;
        _premios = premios;
        _boletos = boletos;
        _chat = chat;
        _configuracion = configuracion;
        _db = db;
        _clock = clock;
    }

    public async Task<Result<LoginAndroidResponse>> LoginMobAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var login = await _auth.LoginAsync(request, cancellationToken);
        if (!login.IsSuccess || login.Data is null)
        {
            return Result<LoginAndroidResponse>.Fail(login.Message, login.StatusCode);
        }

        return Result<LoginAndroidResponse>.Ok(await EnriquecerAsync(login.Data, cancellationToken), login.Message);
    }

    public async Task<Result<LoginAndroidResponse>> CambiarPasswordMobAsync(Guid usuarioId, Guid sesionId, CambiarPasswordRequest request, CancellationToken cancellationToken)
    {
        var login = await _auth.CambiarPasswordAsync(usuarioId, sesionId, request, cancellationToken);
        if (!login.IsSuccess || login.Data is null)
        {
            return Result<LoginAndroidResponse>.Fail(login.Message, login.StatusCode);
        }

        return Result<LoginAndroidResponse>.Ok(await EnriquecerAsync(login.Data, cancellationToken), login.Message);
    }

    public Task<Result> LogoutMobAsync(Guid sesionId, CancellationToken cancellationToken) =>
        _auth.LogoutAsync(sesionId, cancellationToken);

    public async Task<Result<LoginAndroidResponse>> PerfilMobAsync(Guid usuarioId, Guid? dispositivoId, CancellationToken cancellationToken)
    {
        var usuario = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);
        if (usuario is null)
        {
            return Result<LoginAndroidResponse>.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        var baseLogin = new LoginResponse
        {
            UsuarioId = usuario.UsuarioId,
            NombreUsuario = usuario.NombreUsuario,
            NombreCompleto = usuario.NombreCompleto,
            Rol = usuario.Rol,
            DebeCambiarPassword = usuario.EstadoValidado,
            DispositivoId = dispositivoId
        };
        return Result<LoginAndroidResponse>.Ok(await EnriquecerAsync(baseLogin, cancellationToken), SuccessMessages.OperacionExitosa);
    }

    public Task<Result<ConfiguracionOperativaResponse>> ObtenerOperativaMobAsync(CancellationToken cancellationToken) =>
        _configuracion.ObtenerOperativaAsync(cancellationToken);

    public Task<Result<IReadOnlyList<LoteriaResponse>>> LoteriasMobAsync(CancellationToken cancellationToken) =>
        _loterias.ListarAsync(cancellationToken);

    public Task<Result<VentaResponse>> ConfirmarVentaMobAsync(Guid vendedorId, Guid? dispositivoId, ConfirmarVentaRequest request, string? idempotencyKey, CancellationToken cancellationToken) =>
        _ventas.ConfirmarAsync(vendedorId, dispositivoId, request, idempotencyKey, cancellationToken);

    public Task<Result<IReadOnlyList<VentaResponse>>> ConsultarVentasMobAsync(ConsultaVentasRequest request, Guid solicitanteId, bool soloPropias, CancellationToken cancellationToken) =>
        _ventas.ConsultarAsync(request, solicitanteId, soloPropias, cancellationToken);

    public Task<Result<IReadOnlyList<ResultadoResponse>>> ResultadosMobAsync(DateOnly? fecha, Guid? loteriaId, CancellationToken cancellationToken) =>
        _resultados.ListarAsync(fecha, loteriaId, cancellationToken);

    public Task<Result<CasoGanadorResponse>> ReportarPremioMobAsync(Guid solicitanteId, ReportarCasoGanadorRequest request, CancellationToken cancellationToken) =>
        _premios.ReportarAsync(solicitanteId, request, cancellationToken);

    public Task<Result<DescargaAdjuntoResponse>> ObtenerFotoPremioMobAsync(Guid casoId, CancellationToken cancellationToken) =>
        _premios.ObtenerFotoAsync(casoId, cancellationToken);

    public Task<Result<IReadOnlyList<CasoGanadorResponse>>> PremiosMobAsync(CancellationToken cancellationToken) =>
        _premios.ListarAsync(cancellationToken);

    public Task<Result<ValidacionBoletoResponse>> ValidarQrMobAsync(ValidarQrRequest request, CancellationToken cancellationToken) =>
        _boletos.ValidarQrAsync(request, cancellationToken);

    public Task<Result<ConsultaTicketResponse>> ConsultarTicketMobAsync(string? ticketCode, CancellationToken cancellationToken) =>
        _boletos.ConsultarPorCodigoAsync(ticketCode, cancellationToken);

    public Task<Result<IReadOnlyList<BoletoListaResponse>>> FiltrarBoletosMobAsync(FiltroBoletosRequest request, CancellationToken cancellationToken) =>
        _boletos.FiltrarAsync(request, cancellationToken);

    public Task<Result<TirillaResponse>> TirillaMobAsync(Guid boletoId, CancellationToken cancellationToken) =>
        _boletos.ObtenerTirillaAsync(boletoId, cancellationToken);

    public Task<Result<ConversacionResponse>> IniciarChatMobAsync(Guid iniciadorId, IniciarChatRequest request, CancellationToken cancellationToken) =>
        _chat.IniciarAsync(iniciadorId, request, cancellationToken);

    public Task<Result<IReadOnlyList<ConversacionResponse>>> ListarChatMobAsync(Guid usuarioId, CancellationToken cancellationToken) =>
        _chat.ListarAsync(usuarioId, cancellationToken);

    public Task<Result<ConversacionDetalleResponse>> ObtenerChatMobAsync(Guid conversacionId, Guid usuarioId, CancellationToken cancellationToken) =>
        _chat.ObtenerAsync(conversacionId, usuarioId, cancellationToken);

    public Task<Result<MensajeResponse>> EnviarChatMobAsync(Guid conversacionId, Guid emisorId, EnviarMensajeRequest request, CancellationToken cancellationToken) =>
        _chat.EnviarAsync(conversacionId, emisorId, request, cancellationToken);

    public Task<Result<DescargaAdjuntoResponse>> DescargarAdjuntoMobAsync(Guid adjuntoId, Guid usuarioId, CancellationToken cancellationToken) =>
        _chat.DescargarAdjuntoAsync(adjuntoId, usuarioId, cancellationToken);

    public async Task<Result<IReadOnlyList<CodigoOfflineAndroidResponse>>> DescargarOfflineMobAsync(Guid usuarioId, Guid? dispositivoId, CancellationToken cancellationToken)
    {
        if (!dispositivoId.HasValue)
        {
            return Result<IReadOnlyList<CodigoOfflineAndroidResponse>>.Fail(AuthMessages.DispositivoNoAsociado);
        }

        var pendientes = await _db.CodigosPreventaOffline
            .Where(c => c.UsuarioId == usuarioId
                && c.DispositivoId == dispositivoId.Value
                && (c.EstadoDelCodigo == EstadoCodigoOffline.Generado || c.EstadoDelCodigo == EstadoCodigoOffline.Descargado))
            .ToListAsync(cancellationToken);

        foreach (var codigo in pendientes.Where(c => c.EstadoDelCodigo == EstadoCodigoOffline.Generado))
        {
            codigo.EstadoDelCodigo = EstadoCodigoOffline.Descargado;
            codigo.FechaDescarga = _clock.UtcNow;
        }

        if (pendientes.Count == 0)
        {
            return Result<IReadOnlyList<CodigoOfflineAndroidResponse>>.Ok(
                Array.Empty<CodigoOfflineAndroidResponse>(),
                UsuarioMessages.SinCodigosOfflineDisponibles);
        }

        await _db.SaveChangesAsync(cancellationToken);

        var datos = pendientes.Select(c => new CodigoOfflineAndroidResponse
        {
            CodigoId = c.CodigoId,
            Consecutivo = c.ConsecutivoUnico,
            PayloadBase64 = Convert.ToBase64String(c.PayloadCifrado)
        }).ToArray();

        return Result<IReadOnlyList<CodigoOfflineAndroidResponse>>.Ok(datos, SuccessMessages.CodigosOfflineDescargados);
    }

    private async Task<LoginAndroidResponse> EnriquecerAsync(LoginResponse login, CancellationToken cancellationToken)
    {
        var grupoNombre = await _db.UsuariosGrupos
            .AsNoTracking()
            .Where(x => x.UsuarioId == login.UsuarioId)
            .Select(x => x.Grupo!.Nombre)
            .FirstOrDefaultAsync(cancellationToken);

        var codigoPda = login.DispositivoId.HasValue
            ? await _db.Dispositivos.AsNoTracking()
                .Where(d => d.DispositivoId == login.DispositivoId)
                .Select(d => d.CodigoDispositivo)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return new LoginAndroidResponse
        {
            Token = login.Token,
            UsuarioId = login.UsuarioId,
            NombreUsuario = login.NombreUsuario,
            NombreCompleto = login.NombreCompleto,
            Rol = login.Rol,
            DebeCambiarPassword = login.DebeCambiarPassword,
            DispositivoId = login.DispositivoId,
            FechaExpiracion = login.FechaExpiracion,
            GrupoNombre = grupoNombre ?? string.Empty,
            CodigoDispositivo = codigoPda ?? string.Empty
        };
    }
}
