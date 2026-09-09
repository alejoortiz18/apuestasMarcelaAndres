using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class ConfiguracionService : IConfiguracionService
{
    private static readonly (string Clave, string Valor)[] ClavesDefecto =
    [
        (ConfiguracionClaves.HoraCierre, "20:00:00"),
        (ConfiguracionClaves.VigenciaPremiosDias, "30"),
        (ConfiguracionClaves.AlertaRepeticionNumero, "10"),
        (ConfiguracionClaves.AlertaValorMinimo, "10000"),
        (ConfiguracionClaves.CodigosOfflineCapacidad, "3000"),
        (ConfiguracionClaves.SincronizacionModo, ConfiguracionClaves.ModoManual)
    ];

    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public ConfiguracionService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<ConfiguracionResponse>>> ListarAsync(CancellationToken cancellationToken)
    {
        await AsegurarValoresAsync(cancellationToken);
        var items = await _db.Configuraciones.OrderBy(x => x.Clave).ToListAsync(cancellationToken);
        return Result<IReadOnlyList<ConfiguracionResponse>>.Ok(items.Select(Map).ToList(), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<ConfiguracionResponse>> ActualizarAsync(string clave, string valor, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clave))
        {
            return Result<ConfiguracionResponse>.Fail(ValidationMessages.ClaveConfiguracionRequerida);
        }

        var operativa = await ObtenerOperativaAsync(cancellationToken);
        if (!operativa.IsSuccess || operativa.Data is null)
        {
            return Result<ConfiguracionResponse>.Fail(operativa.Message);
        }

        var request = AplicarClave(operativa.Data, clave.Trim(), valor);
        if (request is null)
        {
            return Result<ConfiguracionResponse>.Fail(ConfiguracionMessages.ConfiguracionNoEncontrada, 404);
        }

        var guardado = await GuardarOperativaAsync(request, cancellationToken);
        if (!guardado.IsSuccess)
        {
            return Result<ConfiguracionResponse>.Fail(guardado.Message);
        }

        var item = await _db.Configuraciones.FirstAsync(x => x.Clave == clave.Trim(), cancellationToken);
        return Result<ConfiguracionResponse>.Ok(Map(item), SuccessMessages.RegistroActualizado);
    }

    public async Task<Result<ConfiguracionOperativaResponse>> ObtenerOperativaAsync(CancellationToken cancellationToken)
    {
        await AsegurarValoresAsync(cancellationToken);
        var mapa = await _db.Configuraciones.ToDictionaryAsync(x => x.Clave, x => x.Valor, cancellationToken);
        var combinado = await _db.ConfiguracionesTipoApuesta.FirstAsync(x => x.TipoApuesta == ConfiguracionClaves.TipoCombinado, cancellationToken);
        var individual = await _db.ConfiguracionesTipoApuesta.FirstAsync(x => x.TipoApuesta == ConfiguracionClaves.TipoIndividual, cancellationToken);
        return Result<ConfiguracionOperativaResponse>.Ok(new ConfiguracionOperativaResponse
        {
            HoraCierre = mapa[ConfiguracionClaves.HoraCierre],
            VigenciaPremiosDias = Entero(mapa[ConfiguracionClaves.VigenciaPremiosDias], 30),
            MaxJuegosCombinado = combinado.Maximo,
            MaxLineasIndividual = individual.Maximo,
            AlertaRepeticionNumero = Entero(mapa[ConfiguracionClaves.AlertaRepeticionNumero], 10),
            AlertaValorMinimo = Entero(mapa[ConfiguracionClaves.AlertaValorMinimo], 10000),
            CodigosOfflineCapacidad = Entero(mapa[ConfiguracionClaves.CodigosOfflineCapacidad], 3000),
            SincronizacionModo = mapa[ConfiguracionClaves.SincronizacionModo]
        }, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<ConfiguracionOperativaResponse>> GuardarOperativaAsync(GuardarConfiguracionOperativaRequest request, CancellationToken cancellationToken)
    {
        if (!TimeSpan.TryParse(request.HoraCierre, out var hora))
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ConfiguracionMessages.HoraCierreInvalida);
        }

        if (request.VigenciaPremiosDias <= 0)
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ConfiguracionMessages.VigenciaInvalida);
        }

        if (request.MaxJuegosCombinado <= 0 || request.AlertaRepeticionNumero <= 0)
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ConfiguracionMessages.EnteroInvalido);
        }

        if (request.MaxLineasIndividual is < 1 or > 6)
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ConfiguracionMessages.LineasIndividualInvalidas);
        }

        if (request.AlertaValorMinimo < 0)
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ConfiguracionMessages.AlertaValorInvalida);
        }

        if (request.CodigosOfflineCapacidad is < 3000 or > 5000)
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ValidationMessages.CapacidadCodigosOfflineRango);
        }

        var modo = (request.SincronizacionModo ?? string.Empty).Trim();
        if (!modo.Equals(ConfiguracionClaves.ModoManual, StringComparison.OrdinalIgnoreCase)
            && !modo.Equals(ConfiguracionClaves.ModoAutomatica, StringComparison.OrdinalIgnoreCase))
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ConfiguracionMessages.ModoSincronizacionInvalido);
        }

        modo = modo.Equals(ConfiguracionClaves.ModoAutomatica, StringComparison.OrdinalIgnoreCase)
            ? ConfiguracionClaves.ModoAutomatica
            : ConfiguracionClaves.ModoManual;

        await AsegurarValoresAsync(cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.HoraCierre, hora.ToString(@"hh\:mm\:ss"), cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.VigenciaPremiosDias, request.VigenciaPremiosDias.ToString(), cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.AlertaRepeticionNumero, request.AlertaRepeticionNumero.ToString(), cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.AlertaValorMinimo, request.AlertaValorMinimo.ToString(), cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.CodigosOfflineCapacidad, request.CodigosOfflineCapacidad.ToString(), cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.SincronizacionModo, modo, cancellationToken);
        await GuardarTipoAsync(ConfiguracionClaves.TipoCombinado, request.MaxJuegosCombinado, cancellationToken);
        await GuardarTipoAsync(ConfiguracionClaves.TipoIndividual, request.MaxLineasIndividual, cancellationToken);

        var pdas = await _db.Dispositivos.ToListAsync(cancellationToken);
        foreach (var pda in pdas)
        {
            pda.CapacidadCodigosOffline = request.CodigosOfflineCapacidad;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await ObtenerOperativaAsync(cancellationToken);
    }

    private async Task AsegurarValoresAsync(CancellationToken cancellationToken)
    {
        var existentes = await _db.Configuraciones.Select(x => x.Clave).ToListAsync(cancellationToken);
        foreach (var (clave, valor) in ClavesDefecto)
        {
            if (existentes.Contains(clave))
            {
                continue;
            }

            _db.Configuraciones.Add(new Configuracion
            {
                ConfiguracionId = Guid.NewGuid(),
                Clave = clave,
                Valor = valor,
                FechaActualizacion = _clock.UtcNow
            });
        }

        await AsegurarTipoAsync(ConfiguracionClaves.TipoCombinado, 1, cancellationToken);
        await AsegurarTipoAsync(ConfiguracionClaves.TipoIndividual, 6, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task AsegurarTipoAsync(string tipo, int maximo, CancellationToken cancellationToken)
    {
        if (await _db.ConfiguracionesTipoApuesta.AnyAsync(x => x.TipoApuesta == tipo, cancellationToken))
        {
            return;
        }

        _db.ConfiguracionesTipoApuesta.Add(new ConfiguracionTipoApuesta
        {
            ConfiguracionTipoApuestaId = Guid.NewGuid(),
            TipoApuesta = tipo,
            Maximo = maximo,
            FechaActualizacion = _clock.UtcNow
        });
    }

    private async Task GuardarClaveAsync(string clave, string valor, CancellationToken cancellationToken)
    {
        var item = await _db.Configuraciones.FirstAsync(x => x.Clave == clave, cancellationToken);
        item.Valor = valor;
        item.FechaActualizacion = _clock.UtcNow;
    }

    private async Task GuardarTipoAsync(string tipo, int maximo, CancellationToken cancellationToken)
    {
        var item = await _db.ConfiguracionesTipoApuesta.FirstAsync(x => x.TipoApuesta == tipo, cancellationToken);
        item.Maximo = maximo;
        item.FechaActualizacion = _clock.UtcNow;
    }

    private static GuardarConfiguracionOperativaRequest ToRequest(ConfiguracionOperativaResponse actual) => new()
    {
        HoraCierre = actual.HoraCierre,
        VigenciaPremiosDias = actual.VigenciaPremiosDias,
        MaxJuegosCombinado = actual.MaxJuegosCombinado,
        MaxLineasIndividual = actual.MaxLineasIndividual,
        AlertaRepeticionNumero = actual.AlertaRepeticionNumero,
        AlertaValorMinimo = actual.AlertaValorMinimo,
        CodigosOfflineCapacidad = actual.CodigosOfflineCapacidad,
        SincronizacionModo = actual.SincronizacionModo
    };

    private static GuardarConfiguracionOperativaRequest? AplicarClave(ConfiguracionOperativaResponse actual, string clave, string valor)
    {
        var request = ToRequest(actual);
        return clave switch
        {
            ConfiguracionClaves.HoraCierre => request with { HoraCierre = valor },
            ConfiguracionClaves.VigenciaPremiosDias when int.TryParse(valor, out var vigencia) => request with { VigenciaPremiosDias = vigencia },
            ConfiguracionClaves.AlertaRepeticionNumero when int.TryParse(valor, out var repeticion) => request with { AlertaRepeticionNumero = repeticion },
            ConfiguracionClaves.AlertaValorMinimo when int.TryParse(valor, out var minimo) => request with { AlertaValorMinimo = minimo },
            ConfiguracionClaves.CodigosOfflineCapacidad when int.TryParse(valor, out var capacidad) => request with { CodigosOfflineCapacidad = capacidad },
            ConfiguracionClaves.SincronizacionModo => request with { SincronizacionModo = valor },
            _ => null
        };
    }

    private static int Entero(string valor, int defecto) => int.TryParse(valor, out var n) ? n : defecto;

    private static ConfiguracionResponse Map(Configuracion x) => new()
    {
        ConfiguracionId = x.ConfiguracionId,
        Clave = x.Clave,
        Valor = x.Valor
    };
}
