using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Ventas;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class VentaManualServiceTests
{
    private static readonly DateTime Ahora = new(2026, 9, 9, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ConfirmarAsync_guarda_en_el_esquema_manual_y_no_crea_venta_pda()
    {
        var (sut, db) = await CreateSutAsync();
        var vendedor = db.Usuarios.Single(u => u.Rol == RolUsuario.Vendedor);
        var loteria = db.Loterias.Single();

        var result = await sut.ConfirmarAsync(
            vendedor.UsuarioId,
            new ConfirmarVentaRequest
            {
                TipoApuesta = TipoApuesta.INDIVIDUAL,
                Juegos =
                [
                    new LineaJuegoRequest
                    {
                        Numero = "1234",
                        Valor = 1000,
                        LoteriaIds = [loteria.LoteriaId]
                    }
                ]
            },
            Guid.NewGuid().ToString("N"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Data!.CodigoPublico.Should().NotBeNullOrWhiteSpace();
        (await db.Ventas.CountAsync()).Should().Be(0);
        (await db.VentasManual.CountAsync()).Should().Be(1);
        (await db.VentasManual.SingleAsync()).UsuarioId.Should().Be(vendedor.UsuarioId);
    }

    [Fact]
    public async Task ConfirmarAsync_rechaza_mas_lineas_que_el_limite_leido_de_la_configuracion()
    {
        var (sut, db) = await CreateSutAsync();
        var vendedor = db.Usuarios.Single(u => u.Rol == RolUsuario.Vendedor);
        var loteria = db.Loterias.Single();
        var limite = db.ConfiguracionesTipoApuesta.Single(t => t.TipoApuesta == "INDIVIDUAL");
        limite.Maximo = 1;
        await db.SaveChangesAsync();

        var result = await sut.ConfirmarAsync(
            vendedor.UsuarioId,
            new ConfirmarVentaRequest
            {
                TipoApuesta = TipoApuesta.INDIVIDUAL,
                Juegos =
                [
                    new LineaJuegoRequest { Numero = "1111", Valor = 1000, LoteriaIds = [loteria.LoteriaId] },
                    new LineaJuegoRequest { Numero = "2222", Valor = 1000, LoteriaIds = [loteria.LoteriaId] }
                ]
            },
            Guid.NewGuid().ToString("N"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(VentaMessages.MaximoLineasExcedido);
        (await db.VentasManual.CountAsync()).Should().Be(0);
        (await db.Ventas.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ConfirmarAsync_individual_exige_una_sola_loteria_por_linea()
    {
        var (sut, db) = await CreateSutAsync();
        var vendedor = db.Usuarios.Single(u => u.Rol == RolUsuario.Vendedor);
        var loterias = db.Loterias.Select(l => l.LoteriaId).ToList();
        db.Loterias.Add(new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Bogota",
            Estado = EstadoGeneral.Activo,
            Tope = 1_000_000m,
            FechaCreacion = Ahora
        });
        await db.SaveChangesAsync();
        var segunda = db.Loterias.Single(l => l.Nombre == "Bogota").LoteriaId;

        var result = await sut.ConfirmarAsync(
            vendedor.UsuarioId,
            new ConfirmarVentaRequest
            {
                TipoApuesta = TipoApuesta.INDIVIDUAL,
                Juegos =
                [
                    new LineaJuegoRequest
                    {
                        Numero = "1234",
                        Valor = 1000,
                        LoteriaIds = [loterias[0], segunda]
                    }
                ]
            },
            Guid.NewGuid().ToString("N"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(VentaMessages.IndividualUnaLoteria);
        (await db.VentasManual.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RegistrarNumerosAsync_reparte_el_valor_en_el_esquema_manual()
    {
        var (sut, db) = await CreateSutAsync();
        var vendedor = db.Usuarios.Single(u => u.Rol == RolUsuario.Vendedor);
        var cali = db.Loterias.Single().LoteriaId;
        var bogota = Guid.NewGuid();
        db.Loterias.Add(new Loteria
        {
            LoteriaId = bogota,
            Nombre = "Bogota",
            Estado = EstadoGeneral.Activo,
            Tope = 1_000_000m,
            FechaCreacion = Ahora
        });
        foreach (var dia in DiasVentaLoteria.Semana)
        {
            db.LoteriasDiasSemana.Add(new LoteriaDiaSemana
            {
                LoteriaId = bogota,
                DiaSemana = dia,
                FechaActualizacion = Ahora
            });
        }

        await db.SaveChangesAsync();

        var result = await sut.RegistrarNumerosAsync(
            vendedor.UsuarioId,
            new NewRich.Application.Contracts.JuegoNumerosManual.RegistrarJuegoNumerosManualRequest
            {
                VentaId = Guid.NewGuid(),
                ValorJugado = 10,
                Numeros = ["1234", "5678"],
                LoteriaIds = [cali, bogota]
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Data!.Juegos.Should().HaveCount(4);
        result.Data.Total.Should().Be(10);
        (await db.Ventas.CountAsync()).Should().Be(0);
        (await db.VentasManual.CountAsync()).Should().Be(1);
    }

    private static async Task<(VentaManualService Sut, NewRichDbContext Db)> CreateSutAsync()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        db.Usuarios.Add(new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "Luis Vendedor",
            NombreUsuario = "luis",
            Alias = "Luis",
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Vendedor,
            Estado = EstadoUsuario.Activo,
            FechaCreacion = Ahora
        });
        var loteria = new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Cali",
            Estado = EstadoGeneral.Activo,
            Tope = 1_000_000m,
            FechaCreacion = Ahora
        };
        db.Loterias.Add(loteria);
        foreach (var dia in DiasVentaLoteria.Semana)
        {
            db.LoteriasDiasSemana.Add(new LoteriaDiaSemana
            {
                LoteriaId = loteria.LoteriaId,
                DiaSemana = dia,
                FechaActualizacion = Ahora
            });
        }

        db.ConfiguracionesTipoApuesta.Add(new ConfiguracionTipoApuesta
        {
            ConfiguracionTipoApuestaId = Guid.NewGuid(),
            TipoApuesta = "INDIVIDUAL",
            Maximo = 6,
            FechaActualizacion = Ahora
        });
        db.Configuraciones.Add(new Configuracion
        {
            ConfiguracionId = Guid.NewGuid(),
            Clave = "VigenciaPremiosDias",
            Valor = "30",
            FechaActualizacion = Ahora
        });
        await db.SaveChangesAsync();
        var reloj = new RelojFijo(Ahora);
        var sut = new VentaManualService(db, new VentaManualRepositorio(db), reloj);
        return (sut, db);
    }

    private sealed class RelojFijo : IClock
    {
        public RelojFijo(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => ZonaHorariaColombia.ALocal(UtcNow);
    }
}
