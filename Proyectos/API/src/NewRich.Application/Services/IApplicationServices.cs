using NewRich.Application.Abstractions;
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
using NewRich.Application.Contracts.Offline;
using NewRich.Application.Contracts.Premios;
using NewRich.Application.Contracts.Resultados;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Application.Contracts.Ventas;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<Result<LoginResponse>> CambiarPasswordAsync(Guid usuarioId, Guid sesionId, CambiarPasswordRequest request, CancellationToken cancellationToken);
    Task<Result> LogoutAsync(Guid sesionId, CancellationToken cancellationToken);
}

public interface IUsuarioService
{
    Task<Result<IReadOnlyList<UsuarioResponse>>> ListarAsync(CancellationToken cancellationToken);
    Task<Result<UsuarioResponse>> ObtenerAsync(Guid usuarioId, CancellationToken cancellationToken);
    Task<Result<UsuarioResponse>> CrearAsync(CrearUsuarioRequest request, CancellationToken cancellationToken);
    Task<Result<UsuarioResponse>> ActualizarAsync(Guid usuarioId, ActualizarUsuarioRequest request, CancellationToken cancellationToken);
    Task<Result> EliminarAsync(Guid usuarioId, Guid solicitanteId, CancellationToken cancellationToken);
    Task<Result<RestablecerPasswordResponse>> RestablecerPasswordAsync(Guid usuarioId, CancellationToken cancellationToken);
    Task<Result> DesbloquearAsync(Guid usuarioId, CancellationToken cancellationToken);
}

public interface IDispositivoService
{
    Task<Result<IReadOnlyList<DispositivoResponse>>> ListarAsync(CancellationToken cancellationToken);
    Task<Result<DispositivoResponse>> CrearAsync(CrearDispositivoRequest request, CancellationToken cancellationToken);
    Task<Result<DispositivoResponse>> ActualizarAsync(Guid dispositivoId, ActualizarDispositivoRequest request, CancellationToken cancellationToken);
    Task<Result> AsociarAsync(Guid dispositivoId, Guid usuarioId, CancellationToken cancellationToken);
    Task<Result> DesasociarAsync(Guid dispositivoId, Guid usuarioId, CancellationToken cancellationToken);
    Task<Result> DesasociarAsync(Guid dispositivoId, CancellationToken cancellationToken);
    Task<Result> EliminarAsync(Guid dispositivoId, CancellationToken cancellationToken);
}

public interface ILoteriaService
{
    Task<Result<IReadOnlyList<LoteriaResponse>>> ListarAsync(CancellationToken cancellationToken);
    Task<Result<LoteriaResponse>> CrearAsync(CrearLoteriaRequest request, CancellationToken cancellationToken);
    Task<Result<LoteriaResponse>> ActualizarAsync(Guid loteriaId, ActualizarLoteriaRequest request, CancellationToken cancellationToken);
}

public interface IGrupoService
{
    Task<Result<IReadOnlyList<GrupoResponse>>> ListarAsync(CancellationToken cancellationToken);
    Task<Result<GrupoResponse>> ObtenerAsync(Guid grupoId, CancellationToken cancellationToken);
    Task<Result<GrupoResponse>> CrearAsync(CrearGrupoRequest request, CancellationToken cancellationToken);
    Task<Result<GrupoResponse>> ActualizarAsync(Guid grupoId, ActualizarGrupoRequest request, CancellationToken cancellationToken);
    Task<Result> EliminarAsync(Guid grupoId, CancellationToken cancellationToken);
    Task<Result> AsignarVendedorAsync(Guid usuarioId, Guid? grupoId, CancellationToken cancellationToken);
}

public interface IConfiguracionService
{
    Task<Result<IReadOnlyList<ConfiguracionResponse>>> ListarAsync(CancellationToken cancellationToken);
    Task<Result<ConfiguracionResponse>> ActualizarAsync(string clave, string valor, CancellationToken cancellationToken);
    Task<Result<ConfiguracionOperativaResponse>> ObtenerOperativaAsync(CancellationToken cancellationToken);
    Task<Result<ConfiguracionOperativaResponse>> GuardarOperativaAsync(GuardarConfiguracionOperativaRequest request, CancellationToken cancellationToken);
}

public interface IVentaService
{
    Task<Result<VentaResponse>> ConfirmarAsync(Guid vendedorId, Guid? dispositivoId, ConfirmarVentaRequest request, string? idempotencyKey, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<VentaResponse>>> ConsultarAsync(ConsultaVentasRequest request, Guid solicitanteId, bool soloPropias, CancellationToken cancellationToken);
}

public interface IResultadoService
{
    Task<Result<ResultadoResponse>> RegistrarAsync(RegistrarResultadoRequest request, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ResultadoResponse>>> ListarAsync(DateOnly? fecha, Guid? loteriaId, CancellationToken cancellationToken);
}

public interface IValidacionBoletoService
{
    Task<Result<ValidacionBoletoResponse>> ValidarQrAsync(ValidarQrRequest request, CancellationToken cancellationToken);
    Task<Result<ValidacionBoletoResponse>> AutorizarPagoAsync(Guid boletoId, CancellationToken cancellationToken);
    Task<Result<TirillaResponse>> ObtenerTirillaAsync(Guid boletoId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<BoletoListaResponse>>> FiltrarAsync(FiltroBoletosRequest request, CancellationToken cancellationToken);
}

public interface INotificacionService
{
    Task<Result<NotificacionesResponse>> ListarAsync(Guid usuarioId, CancellationToken cancellationToken);
    Task<Result> MarcarLeidasAsync(Guid usuarioId, CancellationToken cancellationToken);
}

public interface IKpiService
{
    Task<Result<KpiResponse>> ConsultarAsync(KpiRequest request, CancellationToken cancellationToken);
}

public interface IConsultaService
{
    Task<Result<IReadOnlyList<BusquedaAdministrativaResponse>>> BuscarAsync(BusquedaAdministrativaRequest request, CancellationToken cancellationToken);
}

public interface IChatService
{
    Task<Result<ConversacionResponse>> IniciarAsync(Guid iniciadorId, IniciarChatRequest request, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<ConversacionResponse>>> ListarAsync(Guid usuarioId, CancellationToken cancellationToken);
    Task<Result<ConversacionDetalleResponse>> ObtenerAsync(Guid conversacionId, Guid usuarioId, CancellationToken cancellationToken);
    Task<Result<MensajeResponse>> EnviarAsync(Guid conversacionId, Guid emisorId, EnviarMensajeRequest request, CancellationToken cancellationToken);
    Task<Result> CerrarAsync(Guid conversacionId, Guid administradorId, CancellationToken cancellationToken);
    Task<Result<DescargaAdjuntoResponse>> DescargarAdjuntoAsync(Guid adjuntoId, Guid usuarioId, CancellationToken cancellationToken);
}

public interface IOfflineService
{
    Task<Result<OfflineListadoResponse>> ListarAsync(CancellationToken cancellationToken);
    Task<Result<CodigoOfflineResponse>> ObtenerAsync(Guid codigoId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyList<CodigoOfflineResponse>>> GenerarAsync(GenerarCodigosOfflineRequest request, CancellationToken cancellationToken);
}

public interface IPremioService
{
    Task<Result<IReadOnlyList<CasoGanadorResponse>>> ListarAsync(CancellationToken cancellationToken);
    Task<Result<CasoGanadorResponse>> ObtenerAsync(Guid casoId, CancellationToken cancellationToken);
    Task<Result<CasoGanadorResponse>> ReportarAsync(Guid solicitanteId, ReportarCasoGanadorRequest request, CancellationToken cancellationToken);
    Task<Result<CasoGanadorResponse>> ValidarAsync(Guid casoId, Guid adminId, CancellationToken cancellationToken);
    Task<Result<CasoGanadorResponse>> RechazarAsync(Guid casoId, Guid adminId, CancellationToken cancellationToken);
    Task<Result<CasoGanadorResponse>> AsignarAsync(Guid casoId, Guid adminId, AsignarObservadorRequest request, CancellationToken cancellationToken);
}
