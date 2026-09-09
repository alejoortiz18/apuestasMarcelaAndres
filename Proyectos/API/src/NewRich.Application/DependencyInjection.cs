using Microsoft.Extensions.DependencyInjection;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;

namespace NewRich.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioService, UsuarioService>();
        services.AddScoped<IDispositivoService, DispositivoService>();
        services.AddScoped<ILoteriaService, LoteriaService>();
        services.AddScoped<IGrupoService, GrupoService>();
        services.AddScoped<IConfiguracionService, ConfiguracionService>();
        services.AddScoped<IVentaService, VentaService>();
        services.AddScoped<IResultadoService, ResultadoService>();
        services.AddScoped<IValidacionBoletoService, ValidacionBoletoService>();
        services.AddScoped<INotificacionTiempoReal, NotificacionTiempoRealNulo>();
        services.AddScoped<INotificacionService, NotificacionService>();
        services.AddScoped<IKpiService, KpiService>();
        services.AddScoped<IConsultaService, ConsultaService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IOfflineService, OfflineService>();
        services.AddScoped<IPremioService, PremioService>();
        return services;
    }
}
