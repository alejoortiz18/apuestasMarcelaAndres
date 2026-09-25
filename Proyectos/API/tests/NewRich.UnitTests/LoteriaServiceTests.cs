using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class LoteriaServiceTests
{
    [Fact]
    public async Task ListarAsync_sin_ventas_muestra_cero_y_ciudad()
    {
        var (sut, db) = CreateSut();
        db.Configuraciones.Add(new Configuracion
        {
            ConfiguracionId = Guid.NewGuid(),
            Clave = "HoraCierre",
            Valor = "20:00:00",
            FechaActualizacion = DateTime.UtcNow
        });
        db.Loterias.Add(new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Bogota",
            Estado = EstadoGeneral.Activo,
            FechaCreacion = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().ContainSingle(l =>
            l.Nombre == "Bogota"
            && l.NumeroJugado == null
            && l.HoraCierre == "20:00"
            && l.BoletosVendidos == 0
            && l.TotalVendido == 0
            && l.TipoApuesta == null);
    }

    [Fact]
    public async Task ListarAsync_acumula_boletos_y_total_de_la_loteria()
    {
        var (sut, db) = CreateSut();
        var loteria = new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Medellin",
            Estado = EstadoGeneral.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        db.Loterias.Add(loteria);
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "Camila Rojas",
            NombreUsuario = "camila",
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Vendedor,
            FechaCreacion = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);
        var venta = new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = usuario.UsuarioId,
            FechaVenta = DateTime.UtcNow,
            Total = 5000,
            TipoApuesta = TipoApuesta.COMBINADO
        };
        db.Ventas.Add(venta);
        var boleto = new Boleto
        {
            BoletoId = Guid.NewGuid(),
            VentaId = venta.VentaId,
            CodigoPublico = "AOL1234",
            ClaveValidacionHash = "x",
            EstadoBoleto = EstadoBoleto.Jugado,
            FechaCreacion = DateTime.UtcNow,
            VigenciaDias = 30
        };
        db.Boletos.Add(boleto);
        var juego = new Juego
        {
            JuegoId = Guid.NewGuid(),
            BoletoId = boleto.BoletoId,
            Numero = "1234",
            Valor = 5000,
            TipoJuego = TipoJuego.COMBINADA
        };
        db.Juegos.Add(juego);
        db.JuegoLoterias.Add(new JuegoLoteria { JuegoId = juego.JuegoId, LoteriaId = loteria.LoteriaId });
        await db.SaveChangesAsync();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().ContainSingle(l =>
            l.Nombre == "Medellin"
            && l.NumeroJugado == "1234"
            && l.BoletosVendidos == 1
            && l.TotalVendido == 5000
            && l.TipoApuesta == "Combinado");
    }

    [Fact]
    public async Task CrearAsync_exige_al_menos_un_dia_de_juego()
    {
        var (sut, _) = CreateSut();

        var result = await sut.CrearAsync(new CrearLoteriaRequest { Nombre = "Boyaca" }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Debe indicar al menos un día de juego para la lotería.");
    }

    [Fact]
    public async Task CrearAsync_guarda_los_dias_indicados()
    {
        var (sut, _) = CreateSut();

        var creado = await sut.CrearAsync(
            new CrearLoteriaRequest
            {
                Nombre = "Boyaca",
                DiasHabilitados = [DiaSemana.Sabado, DiaSemana.Sabado, DiaSemana.Jueves]
            },
            CancellationToken.None);
        var listado = await sut.ListarAsync(CancellationToken.None);

        creado.IsSuccess.Should().BeTrue(creado.Message);
        listado.Data.Should().ContainSingle(l => l.Nombre == "Boyaca")
            .Which.DiasHabilitados.Should().Equal(DiaSemana.Jueves, DiaSemana.Sabado);
    }

    [Fact]
    public async Task ListarAsync_incluye_los_dias_ya_guardados()
    {
        var (sut, db) = CreateSut();
        var loteria = new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Bogota",
            Estado = EstadoGeneral.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        db.Loterias.Add(loteria);
        db.Set<LoteriaDiaSemana>().AddRange(
            new LoteriaDiaSemana { LoteriaId = loteria.LoteriaId, DiaSemana = DiaSemana.Jueves, FechaActualizacion = DateTime.UtcNow },
            new LoteriaDiaSemana { LoteriaId = loteria.LoteriaId, DiaSemana = DiaSemana.Martes, FechaActualizacion = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.Data.Should().ContainSingle(l => l.Nombre == "Bogota")
            .Which.DiasHabilitados.Should().Equal(DiaSemana.Martes, DiaSemana.Jueves);
    }

    [Fact]
    public async Task ActualizarDiasAsync_reemplaza_los_dias_de_cada_loteria()
    {
        var (sut, _) = CreateSut();
        var creada = await sut.CrearAsync(
            new CrearLoteriaRequest
            {
                Nombre = "Cundinamarca",
                DiasHabilitados = [DiaSemana.Lunes]
            },
            CancellationToken.None);

        var result = await sut.ActualizarDiasAsync(
            new ActualizarDiasLoteriasRequest
            {
                Loterias =
                [
                    new DiasLoteriaRequest
                    {
                        LoteriaId = creada.Data!.LoteriaId,
                        DiasHabilitados = [DiaSemana.Miercoles, DiaSemana.Sabado]
                    }
                ]
            },
            CancellationToken.None);
        var listado = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        listado.Data.Should().ContainSingle(l => l.Nombre == "Cundinamarca")
            .Which.DiasHabilitados.Should().Equal(DiaSemana.Miercoles, DiaSemana.Sabado);
    }

    [Fact]
    public async Task ActualizarTopesAsync_guarda_el_tope_de_todas_las_loterias()
    {
        var (sut, _) = CreateSut();
        var cali = await sut.CrearAsync(new CrearLoteriaRequest { Nombre = "Cali", Tope = 1000, DiasHabilitados = [DiaSemana.Lunes] }, CancellationToken.None);
        var pasto = await sut.CrearAsync(new CrearLoteriaRequest { Nombre = "Pasto", Tope = 1000, DiasHabilitados = [DiaSemana.Lunes] }, CancellationToken.None);

        var result = await sut.ActualizarTopesAsync(
            new ActualizarTopesLoteriasRequest
            {
                Loterias =
                [
                    new TopeLoteriaRequest { LoteriaId = cali.Data!.LoteriaId, Tope = 250000 },
                    new TopeLoteriaRequest { LoteriaId = pasto.Data!.LoteriaId, Tope = 0 }
                ]
            },
            CancellationToken.None);
        var listado = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        listado.Data!.Single(l => l.Nombre == "Cali").Tope.Should().Be(250000);
        listado.Data!.Single(l => l.Nombre == "Pasto").Tope.Should().Be(0);
    }

    [Fact]
    public async Task ActualizarTopesAsync_no_guarda_nada_si_un_tope_es_negativo()
    {
        var (sut, _) = CreateSut();
        var cali = await sut.CrearAsync(new CrearLoteriaRequest { Nombre = "Cali", Tope = 1000, DiasHabilitados = [DiaSemana.Lunes] }, CancellationToken.None);
        var pasto = await sut.CrearAsync(new CrearLoteriaRequest { Nombre = "Pasto", Tope = 1000, DiasHabilitados = [DiaSemana.Lunes] }, CancellationToken.None);

        var result = await sut.ActualizarTopesAsync(
            new ActualizarTopesLoteriasRequest
            {
                Loterias =
                [
                    new TopeLoteriaRequest { LoteriaId = cali.Data!.LoteriaId, Tope = 250000 },
                    new TopeLoteriaRequest { LoteriaId = pasto.Data!.LoteriaId, Tope = -1 }
                ]
            },
            CancellationToken.None);
        var listado = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(VentaMessages.TopeNegativo);
        listado.Data!.Should().OnlyContain(l => l.Tope == 1000);
    }

    private static (LoteriaService Sut, NewRichDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        return (new LoteriaService(db, new FixedClock(DateTime.UtcNow)), db);
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }
}
