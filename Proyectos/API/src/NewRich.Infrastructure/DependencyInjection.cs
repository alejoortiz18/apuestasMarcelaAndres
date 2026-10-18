using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewRich.Application.Abstractions;
using NewRich.Infrastructure.Persistence;
using NewRich.Infrastructure.Security;
using NewRich.Infrastructure.Storage;
using NewRich.Infrastructure.Time;

namespace NewRich.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NewRichDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("NewRichDatabase")));
        services.AddScoped<INewRichDbContext>(sp => sp.GetRequiredService<NewRichDbContext>());
        services.AddScoped<IVentaManualRepositorio, VentaManualRepositorio>();
        services.AddScoped<IRegistrarJuegoManualRepositorio, RegistrarJuegoManualRepositorio>();
        services.AddScoped<IRegistrarJuegoNumerosWebManualRepositorio, RegistrarJuegoNumerosWebManualRepositorio>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IQrCryptoService, AesGcmQrCryptoService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IConfirmacionAccionStore, ConfirmacionAccionBd>();
        if (OpcionesAwsS3.Configurado(configuration))
        {
            services.AddSingleton(_ => OpcionesAwsS3.Cliente(configuration));
            services.AddSingleton<IChatFileStorage, S3ChatFileStorage>();
            services.AddSingleton<IApkAlmacen, S3ApkAlmacen>();
        }
        else
        {
            services.AddSingleton<IChatFileStorage, LocalChatFileStorage>();
            services.AddSingleton<IApkAlmacen, AzureBlobApkAlmacen>();
        }

        return services;
    }
}
