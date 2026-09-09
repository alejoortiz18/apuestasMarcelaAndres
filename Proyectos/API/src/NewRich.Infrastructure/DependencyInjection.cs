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
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IQrCryptoService, AesGcmQrCryptoService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IChatFileStorage, LocalChatFileStorage>();
        return services;
    }
}
