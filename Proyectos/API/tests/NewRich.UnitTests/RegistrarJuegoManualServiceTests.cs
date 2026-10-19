using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.RegistrarJuegoManual;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class RegistrarJuegoManualServiceTests
{
    private static readonly DateTime Ahora = new(2026, 10, 17, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RegistrarAsync_guarda_el_total_escrito_y_un_numero_largo_sin_tocar_las_ventas()
    {
        var (sut, db) = await CreateSutAsync();
        var vendedor = db.Usuarios.Single(u => u.Rol == RolUsuario.Vendedor);
        var loteria = db.Loterias.Single();
        loteria.Estado = EstadoGeneral.Inactivo;
        loteria.Tope = 0;
        await db.SaveChangesAsync();

        var result = await sut.RegistrarAsync(
            vendedor.UsuarioId,
            new RegistrarJuegoManualRequest
            {
                Filas =
                [
                    new FilaRegistrarJuegoManualRequest
                    {
                        LoteriaId = loteria.LoteriaId,
                        Valor = 1000,
                        Numero = "123456789"
                    }
                ]
            },
            Guid.NewGuid().ToString("N"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Data!.Filas.Should().ContainSingle(f => f.Numero == "123456789" && f.Valor == 1000);
        (await db.RegistrosJuegoManual.CountAsync()).Should().Be(1);
        (await db.RegistrosJuegoManual.SingleAsync()).NombreCompleto.Should().Be(vendedor.NombreCompleto);
        (await db.Ventas.CountAsync()).Should().Be(0);
        (await db.VentasManual.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RegistrarAsync_registra_aunque_el_horario_del_sistema_este_cerrado()
    {
        var (sut, db) = await CreateSutAsync();
        var vendedor = db.Usuarios.Single(u => u.Rol == RolUsuario.Vendedor);
        var loteria = db.Loterias.Single();
        db.Configuraciones.AddRange(
            new Configuracion { ConfiguracionId = Guid.NewGuid(), Clave = "HoraApertura", Valor = "18:00", FechaActualizacion = Ahora },
            new Configuracion { ConfiguracionId = Guid.NewGuid(), Clave = "HoraCierre", Valor = "20:00", FechaActualizacion = Ahora });
        await db.SaveChangesAsync();

        var result = await sut.RegistrarAsync(
            vendedor.UsuarioId,
            new RegistrarJuegoManualRequest
            {
                Filas =
                [
                    new FilaRegistrarJuegoManualRequest { LoteriaId = loteria.LoteriaId, Valor = 1000, Numero = "456" }
                ]
            },
            Guid.NewGuid().ToString("N"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        (await db.RegistrosJuegoManual.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ListarLoteriasAsync_incluye_las_inactivas()
    {
        var (sut, db) = await CreateSutAsync();
        db.Loterias.Add(new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Cerrada",
            Estado = EstadoGeneral.Inactivo,
            Tope = 0,
            FechaCreacion = Ahora
        });
        await db.SaveChangesAsync();

        var result = await sut.ListarLoteriasAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Select(l => l.Nombre).Should().BeEquivalentTo("Cali", "Cerrada");
    }

    [Fact]
    public async Task RegistrarAsync_guarda_aunque_el_horario_del_sistema_este_cerrado()
    {
        var (sut, db) = await CreateSutAsync();
        var vendedor = db.Usuarios.Single(u => u.Rol == RolUsuario.Vendedor);
        var loteria = db.Loterias.Single();
        db.Configuraciones.AddRange(
            new Configuracion { ConfiguracionId = Guid.NewGuid(), Clave = "HoraApertura", Valor = "18:00", FechaActualizacion = Ahora },
            new Configuracion { ConfiguracionId = Guid.NewGuid(), Clave = "HoraCierre", Valor = "20:00", FechaActualizacion = Ahora });
        await db.SaveChangesAsync();

        var result = await sut.RegistrarAsync(
            vendedor.UsuarioId,
            new RegistrarJuegoManualRequest
            {
                Filas =
                [
                    new FilaRegistrarJuegoManualRequest { LoteriaId = loteria.LoteriaId, Valor = 1000, Numero = "456" }
                ]
            },
            Guid.NewGuid().ToString("N"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        (await db.RegistrosJuegoManual.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RegistrarAsync_rechaza_un_numero_con_letras()
    {
        var (sut, db) = await CreateSutAsync();
        var vendedor = db.Usuarios.Single(u => u.Rol == RolUsuario.Vendedor);
        var loteria = db.Loterias.Single();

        var result = await sut.RegistrarAsync(
            vendedor.UsuarioId,
            new RegistrarJuegoManualRequest
            {
                Filas =
                [
                    new FilaRegistrarJuegoManualRequest
                    {
                        LoteriaId = loteria.LoteriaId,
                        Valor = 1000,
                        Numero = "12a"
                    }
                ]
            },
            Guid.NewGuid().ToString("N"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(RegistrarJuegoManualMessages.NumeroSoloDigitos);
        (await db.RegistrosJuegoManual.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ConsultarAsync_devuelve_las_filas_registradas_del_vendedor()
    {
        var (sut, db) = await CreateSutAsync();
        var vendedor = db.Usuarios.Single(u => u.Rol == RolUsuario.Vendedor);
        var loteria = db.Loterias.Single();
        await sut.RegistrarAsync(
            vendedor.UsuarioId,
            new RegistrarJuegoManualRequest
            {
                Filas =
                [
                    new FilaRegistrarJuegoManualRequest { LoteriaId = loteria.LoteriaId, Valor = 1500, Numero = "3214" }
                ]
            },
            Guid.NewGuid().ToString("N"),
            CancellationToken.None);

        var result = await sut.ConsultarAsync(
            vendedor.UsuarioId,
            Ahora.Date,
            Ahora.Date.AddDays(1).AddTicks(-1),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        var fila = result.Data.Should().ContainSingle().Subject;
        fila.Loteria.Should().Be("Cali");
        fila.Numero.Should().Be("3214");
        fila.Valor.Should().Be(1500);
        (await db.Ventas.CountAsync()).Should().Be(0);
        (await db.VentasManual.CountAsync()).Should().Be(0);
    }

    private static async Task<(RegistrarJuegoManualService Sut, NewRichDbContext Db)> CreateSutAsync()
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
        db.Loterias.Add(new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Cali",
            Estado = EstadoGeneral.Activo,
            Tope = 1_000_000m,
            FechaCreacion = Ahora
        });
        await db.SaveChangesAsync();
        var sut = new RegistrarJuegoManualService(db, new RegistrarJuegoManualRepositorio(db), new RelojFijo(Ahora));
        return (sut, db);
    }

    private sealed class RelojFijo : IClock
    {
        public RelojFijo(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => ZonaHorariaColombia.ALocal(UtcNow);
    }
}
