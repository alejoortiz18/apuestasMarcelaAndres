using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Constants.Messages;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class ConfiguracionService : IConfiguracionService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public ConfiguracionService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<ConfiguracionResponse>>> ListarAsync(CancellationToken cancellationToken)
    {
        var items = await _db.Configuraciones.OrderBy(x => x.Clave).ToListAsync(cancellationToken);
        return Result<IReadOnlyList<ConfiguracionResponse>>.Ok(items.Select(x => new ConfiguracionResponse
        {
            ConfiguracionId = x.ConfiguracionId,
            Clave = x.Clave,
            Valor = x.Valor
        }).ToList(), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<ConfiguracionResponse>> ActualizarAsync(string clave, string valor, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clave))
        {
            return Result<ConfiguracionResponse>.Fail(ValidationMessages.ClaveConfiguracionRequerida);
        }

        if (string.IsNullOrWhiteSpace(valor))
        {
            return Result<ConfiguracionResponse>.Fail(ValidationMessages.ValorConfiguracionRequerido);
        }

        if (clave == "HoraCierre" && !TimeSpan.TryParse(valor, out _))
        {
            return Result<ConfiguracionResponse>.Fail(ConfiguracionMessages.HoraCierreInvalida);
        }

        if (clave == "VigenciaPremiosDias" && (!int.TryParse(valor, out var dias) || dias <= 0))
        {
            return Result<ConfiguracionResponse>.Fail(ConfiguracionMessages.VigenciaInvalida);
        }

        var item = await _db.Configuraciones.FirstOrDefaultAsync(x => x.Clave == clave, cancellationToken);
        if (item is null)
        {
            return Result<ConfiguracionResponse>.Fail(ConfiguracionMessages.ConfiguracionNoEncontrada, 404);
        }

        item.Valor = valor.Trim();
        item.FechaActualizacion = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Result<ConfiguracionResponse>.Ok(new ConfiguracionResponse
        {
            ConfiguracionId = item.ConfiguracionId,
            Clave = item.Clave,
            Valor = item.Valor
        }, SuccessMessages.RegistroActualizado);
    }
}
