using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Services;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class ConfiguracionService : IConfiguracionService
{
    private static readonly (string Clave, string Valor)[] ClavesDefecto =
    [
        (ConfiguracionClaves.HoraApertura, "10:00:00"),
        (ConfiguracionClaves.HoraCierre, "20:00:00"),
        (ConfiguracionClaves.VigenciaPremiosDias, "30"),
        (ConfiguracionClaves.DiasInactividadEliminarPda, "30"),
        (ConfiguracionClaves.AlertaRepeticionNumero, "10"),
        (ConfiguracionClaves.AlertaValorMinimo, "10000"),
        (ConfiguracionClaves.CodigosOfflineCapacidad, "3000"),
        (ConfiguracionClaves.ReposicionDiariaOffline, "true"),
        (ConfiguracionClaves.PermitirJuegosOffline, "true"),
        (ConfiguracionClaves.SincronizacionModo, ConfiguracionClaves.ModoManual),
        (ConfiguracionClaves.LeyendaTirilla, TirillaCuerpo.CuerpoDefecto),
        (ConfiguracionClaves.MensajeSuperacionTope, ValidacionTope.PlantillaSuperacionDefecto)
    ];

    private readonly INewRichDbContext _db;
    private readonly IClock _clock;
    private readonly ILoteriasTiempoReal _vivo;

    public ConfiguracionService(INewRichDbContext db, IClock clock, ILoteriasTiempoReal vivo)
    {
        _db = db;
        _clock = clock;
        _vivo = vivo;
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
            HoraApertura = mapa[ConfiguracionClaves.HoraApertura],
            HoraCierre = mapa[ConfiguracionClaves.HoraCierre],
            VigenciaPremiosDias = Entero(mapa[ConfiguracionClaves.VigenciaPremiosDias], 30),
            DiasInactividadEliminarPda = Entero(mapa[ConfiguracionClaves.DiasInactividadEliminarPda], InactividadPda.DiasSinActividadPorDefecto),
            MaxJuegosCombinado = combinado.Maximo,
            MaxLineasIndividual = individual.Maximo,
            AlertaRepeticionNumero = Entero(mapa[ConfiguracionClaves.AlertaRepeticionNumero], 10),
            AlertaValorMinimo = Entero(mapa[ConfiguracionClaves.AlertaValorMinimo], 10000),
            CodigosOfflineCapacidad = Entero(mapa[ConfiguracionClaves.CodigosOfflineCapacidad], 3000),
            ReposicionDiariaOffline = Booleano(mapa[ConfiguracionClaves.ReposicionDiariaOffline], true),
            PermitirJuegosOffline = Booleano(mapa[ConfiguracionClaves.PermitirJuegosOffline], true),
            SincronizacionModo = mapa[ConfiguracionClaves.SincronizacionModo],
            LeyendaTirilla = TirillaCuerpo.NormalizarCuerpo(mapa[ConfiguracionClaves.LeyendaTirilla]),
            MensajeSuperacionTope = ValidacionTope.NormalizarPlantilla(mapa[ConfiguracionClaves.MensajeSuperacionTope]),
            NumerosRestringidos = await _db.NumerosRestringidos
                .AsNoTracking()
                .OrderBy(x => x.Numero)
                .Select(x => x.Numero)
                .ToListAsync(cancellationToken),
            TopesLoterias = (await _db.Loterias
                .AsNoTracking()
                .OrderBy(x => x.Nombre)
                .ToListAsync(cancellationToken))
                .Select(x => new TopeLoteriaResponse
                {
                    LoteriaId = x.LoteriaId,
                    Nombre = x.Nombre,
                    Tope = x.Tope,
                    HoraInicio = x.HoraInicio.ToString(@"hh\:mm"),
                    HoraFin = x.HoraFin.ToString(@"hh\:mm")
                })
                .ToList()
        }, SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<ConfiguracionOperativaResponse>> GuardarOperativaAsync(GuardarConfiguracionOperativaRequest request, CancellationToken cancellationToken)
    {
        if (!Hora12.TryParse(request.HoraApertura, out var apertura))
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ConfiguracionMessages.HoraAperturaInvalida);
        }

        if (!Hora12.TryParse(request.HoraCierre, out var cierre))
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ConfiguracionMessages.HoraCierreInvalida);
        }

        if (!HorarioOperacion.SonDistintas(apertura, cierre))
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ConfiguracionMessages.HorasOperacionIguales);
        }

        var loterias = await _db.Loterias.AsNoTracking().ToListAsync(cancellationToken);
        if (loterias.Any(l => !HorarioLoteria.EsValido(l.HoraInicio, l.HoraFin, apertura, cierre)))
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ConfiguracionMessages.HorarioPdaViolaLoterias);
        }

        if (request.VigenciaPremiosDias <= 0)
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ConfiguracionMessages.VigenciaInvalida);
        }

        if (request.DiasInactividadEliminarPda <= 0)
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ConfiguracionMessages.DiasInactividadEliminarPdaInvalido);
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

        if (request.CodigosOfflineCapacidad < 1)
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ValidationMessages.CapacidadCodigosOfflineRango);
        }

        var cuerpo = TirillaCuerpo.NormalizarCuerpo(request.LeyendaTirilla);
        if (string.IsNullOrWhiteSpace(request.LeyendaTirilla) || string.IsNullOrWhiteSpace(cuerpo))
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ConfiguracionMessages.LeyendaTirillaRequerida);
        }

        if (cuerpo.Length > 4000)
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ConfiguracionMessages.LeyendaTirillaDemasiadoLarga);
        }

        if (string.IsNullOrWhiteSpace(request.MensajeSuperacionTope))
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ConfiguracionMessages.MensajeSuperacionTopeRequerido);
        }

        var mensajeTope = ValidacionTope.NormalizarPlantilla(request.MensajeSuperacionTope);
        if (mensajeTope.Length > 4000)
        {
            return Result<ConfiguracionOperativaResponse>.Fail(ConfiguracionMessages.MensajeSuperacionTopeDemasiadoLargo);
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
        await GuardarClaveAsync(ConfiguracionClaves.HoraApertura, apertura.ToString(@"hh\:mm\:ss"), cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.HoraCierre, cierre.ToString(@"hh\:mm\:ss"), cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.VigenciaPremiosDias, request.VigenciaPremiosDias.ToString(), cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.DiasInactividadEliminarPda, request.DiasInactividadEliminarPda.ToString(), cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.AlertaRepeticionNumero, request.AlertaRepeticionNumero.ToString(), cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.AlertaValorMinimo, request.AlertaValorMinimo.ToString(), cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.CodigosOfflineCapacidad, request.CodigosOfflineCapacidad.ToString(), cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.ReposicionDiariaOffline, request.ReposicionDiariaOffline ? "true" : "false", cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.PermitirJuegosOffline, request.PermitirJuegosOffline ? "true" : "false", cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.SincronizacionModo, modo, cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.LeyendaTirilla, cuerpo, cancellationToken);
        await GuardarClaveAsync(ConfiguracionClaves.MensajeSuperacionTope, mensajeTope, cancellationToken);
        await GuardarTipoAsync(ConfiguracionClaves.TipoCombinado, request.MaxJuegosCombinado, cancellationToken);
        await GuardarTipoAsync(ConfiguracionClaves.TipoIndividual, request.MaxLineasIndividual, cancellationToken);

        var pdas = await _db.Dispositivos.ToListAsync(cancellationToken);
        foreach (var pda in pdas)
        {
            pda.CapacidadCodigosOffline = request.CodigosOfflineCapacidad;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _vivo.AvisarCatalogoActualizadoAsync(cancellationToken);
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
        HoraApertura = actual.HoraApertura,
        HoraCierre = actual.HoraCierre,
        VigenciaPremiosDias = actual.VigenciaPremiosDias,
        DiasInactividadEliminarPda = actual.DiasInactividadEliminarPda,
        MaxJuegosCombinado = actual.MaxJuegosCombinado,
        MaxLineasIndividual = actual.MaxLineasIndividual,
        AlertaRepeticionNumero = actual.AlertaRepeticionNumero,
        AlertaValorMinimo = actual.AlertaValorMinimo,
        CodigosOfflineCapacidad = actual.CodigosOfflineCapacidad,
        ReposicionDiariaOffline = actual.ReposicionDiariaOffline,
        PermitirJuegosOffline = actual.PermitirJuegosOffline,
        SincronizacionModo = actual.SincronizacionModo,
        LeyendaTirilla = actual.LeyendaTirilla,
        MensajeSuperacionTope = actual.MensajeSuperacionTope
    };

    private static GuardarConfiguracionOperativaRequest? AplicarClave(ConfiguracionOperativaResponse actual, string clave, string valor)
    {
        var request = ToRequest(actual);
        return clave switch
        {
            ConfiguracionClaves.HoraApertura => request with { HoraApertura = valor },
            ConfiguracionClaves.HoraCierre => request with { HoraCierre = valor },
            ConfiguracionClaves.VigenciaPremiosDias when int.TryParse(valor, out var vigencia) => request with { VigenciaPremiosDias = vigencia },
            ConfiguracionClaves.DiasInactividadEliminarPda when int.TryParse(valor, out var dias) => request with { DiasInactividadEliminarPda = dias },
            ConfiguracionClaves.AlertaRepeticionNumero when int.TryParse(valor, out var repeticion) => request with { AlertaRepeticionNumero = repeticion },
            ConfiguracionClaves.AlertaValorMinimo when int.TryParse(valor, out var minimo) => request with { AlertaValorMinimo = minimo },
            ConfiguracionClaves.CodigosOfflineCapacidad when int.TryParse(valor, out var capacidad) => request with { CodigosOfflineCapacidad = capacidad },
            ConfiguracionClaves.ReposicionDiariaOffline => request with { ReposicionDiariaOffline = Booleano(valor, true) },
            ConfiguracionClaves.PermitirJuegosOffline => request with { PermitirJuegosOffline = Booleano(valor, true) },
            ConfiguracionClaves.SincronizacionModo => request with { SincronizacionModo = valor },
            ConfiguracionClaves.LeyendaTirilla => request with { LeyendaTirilla = valor },
            ConfiguracionClaves.MensajeSuperacionTope => request with { MensajeSuperacionTope = valor },
            _ => null
        };
    }

    private static int Entero(string valor, int defecto) => int.TryParse(valor, out var n) ? n : defecto;

    private static bool Booleano(string? valor, bool defecto)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return defecto;
        }

        if (bool.TryParse(valor, out var b))
        {
            return b;
        }

        return valor.Equals("1", StringComparison.OrdinalIgnoreCase)
            || valor.Equals("si", StringComparison.OrdinalIgnoreCase)
            || valor.Equals("sí", StringComparison.OrdinalIgnoreCase);
    }

    private static ConfiguracionResponse Map(Configuracion x) => new()
    {
        ConfiguracionId = x.ConfiguracionId,
        Clave = x.Clave,
        Valor = x.Valor
    };
}
