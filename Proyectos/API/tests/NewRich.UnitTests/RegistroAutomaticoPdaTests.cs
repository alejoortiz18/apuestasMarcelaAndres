using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Dispositivos;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class RegistroAutomaticoPdaTests
{
    [Fact]
    public async Task Genera_el_codigo_sin_que_nadie_lo_escriba()
    {
        var sut = CreateSut();

        var result = await sut.RegistrarAutomaticoAsync(Solicitud("H10P77624BL0456"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.CodigoDispositivo.Should().NotBeNullOrWhiteSpace();
        result.Data.CodigoDispositivo.Length.Should().BeLessThanOrEqualTo(20);
        result.Data.NumeroSerie.Should().Be("H10P77624BL0456");
        result.Data.Estado.Should().Be(EstadoGeneral.Activo);
    }

    [Fact]
    public async Task Dos_equipos_del_mismo_modelo_reciben_codigos_distintos()
    {
        var sut = CreateSut();

        var primero = await sut.RegistrarAutomaticoAsync(Solicitud("SERIE-A"), CancellationToken.None);
        var segundo = await sut.RegistrarAutomaticoAsync(Solicitud("SERIE-B"), CancellationToken.None);

        primero.IsSuccess.Should().BeTrue();
        segundo.IsSuccess.Should().BeTrue();
        segundo.Data!.CodigoDispositivo.Should().NotBe(primero.Data!.CodigoDispositivo);
    }

    [Fact]
    public async Task Repetir_el_registro_del_mismo_equipo_conserva_su_codigo()
    {
        var sut = CreateSut();
        var primero = await sut.RegistrarAutomaticoAsync(Solicitud("SERIE-A"), CancellationToken.None);

        var repetido = await sut.RegistrarAutomaticoAsync(Solicitud("SERIE-A"), CancellationToken.None);

        repetido.IsSuccess.Should().BeTrue();
        repetido.Data!.CodigoDispositivo.Should().Be(primero.Data!.CodigoDispositivo);
        repetido.Data.DispositivoId.Should().Be(primero.Data.DispositivoId);
    }

    [Fact]
    public async Task Repetir_el_registro_no_duplica_el_dispositivo()
    {
        var (sut, db) = CreateSutWithDb();
        await sut.RegistrarAutomaticoAsync(Solicitud("SERIE-A"), CancellationToken.None);

        await sut.RegistrarAutomaticoAsync(Solicitud("SERIE-A"), CancellationToken.None);

        (await db.Dispositivos.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Cambiar_el_tipo_al_reregistrar_actualiza_el_equipo()
    {
        var sut = CreateSut();
        await sut.RegistrarAutomaticoAsync(Solicitud("SERIE-A"), CancellationToken.None);

        var solicitud = Solicitud("SERIE-A");
        solicitud.Tipo = TipoDispositivo.Observador;
        var result = await sut.RegistrarAutomaticoAsync(solicitud, CancellationToken.None);

        result.Data!.Tipo.Should().Be(TipoDispositivo.Observador);
        result.Message.Should().Be(SuccessMessages.RegistroActualizado);
    }

    [Fact]
    public async Task Exige_el_numero_de_serie_del_equipo()
    {
        var sut = CreateSut();

        var result = await sut.RegistrarAutomaticoAsync(Solicitud("  "), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(ValidationMessages.NumeroSerieRequerido);
    }

    private static RegistrarPdaAutomaticoRequest Solicitud(string serie) => new()
    {
        NumeroSerie = serie,
        Modelo = "H10",
        Tipo = TipoDispositivo.Vendedor
    };

    private static DispositivoService CreateSut() => CreateSutWithDb().Sut;

    private static (DispositivoService Sut, NewRichDbContext Db) CreateSutWithDb()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var sut = new DispositivoService(db, new RelojFijo(), new PresenciaDispositivosMemoria());
        return (sut, db);
    }

    private sealed class RelojFijo : IClock
    {
        public DateTime UtcNow => new(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
        public DateTime LocalNow => UtcNow;
    }
}
