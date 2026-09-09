using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class ConfiguracionServiceTests
{
    private static readonly DateTime Ahora = new(2026, 9, 9, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ObtenerOperativaAsync_crea_claves_faltantes_con_valores_por_defecto()
    {
        var (sut, db) = CreateSut();

        var result = await sut.ObtenerOperativaAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.HoraCierre.Should().Be("20:00:00");
        result.Data.VigenciaPremiosDias.Should().Be(30);
        result.Data.MaxJuegosCombinado.Should().Be(1);
        result.Data.MaxLineasIndividual.Should().Be(6);
        result.Data.AlertaRepeticionNumero.Should().Be(10);
        result.Data.AlertaValorMinimo.Should().Be(10000);
        result.Data.CodigosOfflineCapacidad.Should().Be(3000);
        result.Data.SincronizacionModo.Should().Be("Manual");
        db.Configuraciones.Should().HaveCount(6);
        db.ConfiguracionesTipoApuesta.Should().Contain(t => t.TipoApuesta == "COMBINADO" && t.Maximo == 1);
        db.ConfiguracionesTipoApuesta.Should().Contain(t => t.TipoApuesta == "INDIVIDUAL" && t.Maximo == 6);
    }

    [Fact]
    public async Task ObtenerOperativaAsync_lee_valores_guardados()
    {
        var (sut, db) = CreateSut();
        db.Configuraciones.Add(new Configuracion
        {
            ConfiguracionId = Guid.NewGuid(),
            Clave = ConfiguracionClaves.HoraCierre,
            Valor = "19:30:00",
            FechaActualizacion = Ahora
        });
        db.ConfiguracionesTipoApuesta.Add(new ConfiguracionTipoApuesta
        {
            ConfiguracionTipoApuestaId = Guid.NewGuid(),
            TipoApuesta = "INDIVIDUAL",
            Maximo = 4,
            FechaActualizacion = Ahora
        });
        await db.SaveChangesAsync();

        var result = await sut.ObtenerOperativaAsync(CancellationToken.None);

        result.Data!.HoraCierre.Should().Be("19:30:00");
        result.Data.MaxLineasIndividual.Should().Be(4);
    }

    [Fact]
    public async Task GuardarOperativaAsync_persiste_parametros_y_maximos()
    {
        var (sut, db) = CreateSut();
        await sut.ObtenerOperativaAsync(CancellationToken.None);

        var result = await sut.GuardarOperativaAsync(new GuardarConfiguracionOperativaRequest
        {
            HoraCierre = "18:45",
            VigenciaPremiosDias = 15,
            MaxJuegosCombinado = 2,
            MaxLineasIndividual = 5,
            AlertaRepeticionNumero = 8,
            AlertaValorMinimo = 20000,
            CodigosOfflineCapacidad = 4000,
            SincronizacionModo = "Automatica"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.HoraCierre.Should().Be("18:45:00");
        result.Data.VigenciaPremiosDias.Should().Be(15);
        result.Data.MaxJuegosCombinado.Should().Be(2);
        result.Data.MaxLineasIndividual.Should().Be(5);
        result.Data.AlertaRepeticionNumero.Should().Be(8);
        result.Data.AlertaValorMinimo.Should().Be(20000);
        result.Data.CodigosOfflineCapacidad.Should().Be(4000);
        result.Data.SincronizacionModo.Should().Be("Automatica");
        db.ConfiguracionesTipoApuesta.Single(t => t.TipoApuesta == "COMBINADO").Maximo.Should().Be(2);
        db.ConfiguracionesTipoApuesta.Single(t => t.TipoApuesta == "INDIVIDUAL").Maximo.Should().Be(5);
    }

    [Fact]
    public async Task GuardarOperativaAsync_actualiza_capacidad_de_todos_los_pda()
    {
        var (sut, db) = CreateSut();
        db.Dispositivos.Add(new Dispositivo
        {
            DispositivoId = Guid.NewGuid(),
            CodigoDispositivo = "PDA-001",
            Tipo = TipoDispositivo.Vendedor,
            Estado = EstadoGeneral.Activo,
            CapacidadCodigosOffline = 3000,
            FechaRegistro = Ahora
        });
        await db.SaveChangesAsync();
        await sut.ObtenerOperativaAsync(CancellationToken.None);

        await sut.GuardarOperativaAsync(RequestValida() with { CodigosOfflineCapacidad = 4500 }, CancellationToken.None);

        db.Dispositivos.Single().CapacidadCodigosOffline.Should().Be(4500);
    }

    [Fact]
    public async Task GuardarOperativaAsync_rechaza_hora_invalida()
    {
        var (sut, _) = CreateSut();
        await sut.ObtenerOperativaAsync(CancellationToken.None);

        var result = await sut.GuardarOperativaAsync(RequestValida() with { HoraCierre = "noche" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(ConfiguracionMessages.HoraCierreInvalida);
    }

    [Fact]
    public async Task GuardarOperativaAsync_rechaza_capacidad_fuera_de_rango()
    {
        var (sut, _) = CreateSut();
        await sut.ObtenerOperativaAsync(CancellationToken.None);

        var result = await sut.GuardarOperativaAsync(RequestValida() with { CodigosOfflineCapacidad = 100 }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(ValidationMessages.CapacidadCodigosOfflineRango);
    }

    private static GuardarConfiguracionOperativaRequest RequestValida() => new()
    {
        HoraCierre = "20:00",
        VigenciaPremiosDias = 30,
        MaxJuegosCombinado = 1,
        MaxLineasIndividual = 6,
        AlertaRepeticionNumero = 10,
        AlertaValorMinimo = 10000,
        CodigosOfflineCapacidad = 3000,
        SincronizacionModo = "Manual"
    };

    private static (ConfiguracionService Sut, NewRichDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        return (new ConfiguracionService(db, new RelojFijo(Ahora)), db);
    }

    private sealed class RelojFijo : IClock
    {
        public RelojFijo(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }
}
