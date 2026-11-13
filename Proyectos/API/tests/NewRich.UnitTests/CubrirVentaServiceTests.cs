using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class CubrirVentaServiceTests
{
    private static readonly DateTime Ahora = new(2026, 10, 18, 15, 0, 0);

    [Fact]
    public async Task Resumen_toma_cada_loteria_del_pda_con_el_valor_completo_y_cubrir_una_no_cubre_la_otra()
    {
        var (sut, db, admin) = await CreateSutAsync();
        var desde = new DateTime(2026, 10, 18);
        var hasta = desde.AddDays(1).AddTicks(-1);

        var resumen = await sut.ResumenAsync(desde, hasta, 600, CancellationToken.None);

        resumen.IsSuccess.Should().BeTrue(resumen.Message);
        resumen.Data!.Grupos.Should().HaveCount(2);
        resumen.Data.Grupos.Should().OnlyContain(g => g.Numero == "1234" && g.Subtotal == 1_000);
        resumen.Data.Grupos.Select(g => g.Loteria).Should().BeEquivalentTo("Cali", "Bogotá");
        resumen.Data.Grupos.Should().NotContain(g => g.Numero == "9999");

        var cali = resumen.Data.Grupos.Single(g => g.Loteria == "Cali");
        var cubierto = await sut.CubrirDesdeResumenAsync(admin, cali.Lineas.Select(l => l.Clave).ToList(), 20, 10, CancellationToken.None);

        cubierto.IsSuccess.Should().BeTrue(cubierto.Message);
        cubierto.Message.Should().Be(RevisadaManualMessages.Cubierto);
        cubierto.Data!.Subtotal.Should().Be(1_000);
        cubierto.Data.ComisionValor.Should().Be(200);
        cubierto.Data.TotalConComision.Should().Be(1_200);
        cubierto.Data.DescuentoValor.Should().Be(120);
        cubierto.Data.TotalPagar.Should().Be(1_080);

        var queda = await sut.ResumenAsync(desde, hasta, null, CancellationToken.None);
        queda.Data!.Grupos.Should().ContainSingle(g => g.Loteria == "Bogotá" && g.Numero == "1234" && g.Subtotal == 1_000);
        queda.Data.Grupos.Should().ContainSingle(g => g.Numero == "5678" && g.Subtotal == 500);
        (await db.Juegos.CountAsync()).Should().Be(2);
        (await db.Ventas.CountAsync()).Should().Be(1);
        (await sut.CubrirDesdeResumenAsync(admin, cali.Lineas.Select(l => l.Clave).ToList(), 20, 10, CancellationToken.None)).Message
            .Should().Be(RevisadaManualMessages.YaProcesado);

        db.Usuarios.Add(new Usuario
        {
            UsuarioId = admin,
            NombreCompleto = "Ana Admin",
            NombreUsuario = "admin",
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Administrador,
            Estado = EstadoUsuario.Activo,
            FechaCreacion = desde
        });
        await db.SaveChangesAsync();
        var delPeriodo = await sut.CubiertoAsync(desde, hasta, null, CancellationToken.None);
        delPeriodo.Data!.Grupos.Should().ContainSingle(g => g.Loteria == "Cali" && g.Numero == "1234" && g.Subtotal == 1_000 && g.CubiertoPor == "Ana Admin");
        (await sut.CubiertoAsync(desde, hasta, [Guid.NewGuid()], CancellationToken.None)).Data!.Grupos.Should().BeEmpty();
    }

    private static async Task<(CubrirVentaService Sut, NewRichDbContext Db, Guid Admin)> CreateSutAsync()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var admin = Guid.NewGuid();
        var vendedor = Guid.NewGuid();
        var cali = Guid.NewGuid();
        var bogota = Guid.NewGuid();
        var fecha = new DateTime(2026, 10, 18, 10, 0, 0);
        db.Usuarios.Add(new Usuario
        {
            UsuarioId = vendedor,
            NombreCompleto = "Vendedor PDA",
            NombreUsuario = "vendedor",
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Vendedor,
            Estado = EstadoUsuario.Activo,
            FechaCreacion = fecha
        });
        db.Loterias.Add(Loteria(cali, "Cali", fecha));
        db.Loterias.Add(Loteria(bogota, "Bogotá", fecha));
        var venta = new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = vendedor,
            FechaVenta = fecha,
            Total = 2_500,
            TipoApuesta = TipoApuesta.COMBINADO
        };
        var boleto = new Boleto
        {
            BoletoId = Guid.NewGuid(),
            VentaId = venta.VentaId,
            Venta = venta,
            CodigoPublico = "1234567",
            FechaCreacion = fecha,
            EstadoBoleto = EstadoBoleto.Jugado
        };
        var combo = new Juego
        {
            JuegoId = Guid.NewGuid(),
            BoletoId = boleto.BoletoId,
            Boleto = boleto,
            Numero = "1234",
            Valor = 1_000,
            TipoJuego = TipoJuego.COMBINADA
        };
        combo.JuegoLoterias.Add(new JuegoLoteria { JuegoId = combo.JuegoId, LoteriaId = cali });
        combo.JuegoLoterias.Add(new JuegoLoteria { JuegoId = combo.JuegoId, LoteriaId = bogota });
        var individual = new Juego
        {
            JuegoId = Guid.NewGuid(),
            BoletoId = boleto.BoletoId,
            Boleto = boleto,
            Numero = "5678",
            Valor = 500,
            TipoJuego = TipoJuego.INDIVIDUAL
        };
        individual.JuegoLoterias.Add(new JuegoLoteria { JuegoId = individual.JuegoId, LoteriaId = cali });
        db.Ventas.Add(venta);
        db.Boletos.Add(boleto);
        db.Juegos.Add(combo);
        db.Juegos.Add(individual);

        var web = new RegistroJuegoManual
        {
            RegistroId = Guid.NewGuid(),
            UsuarioId = vendedor,
            NombreCompleto = "Vendedor PDA",
            FechaRegistro = fecha,
            CodigoPublico = "7654321",
            IdempotencyKey = "web"
        };
        web.Filas.Add(new FilaRegistroJuegoManual
        {
            FilaId = Guid.NewGuid(),
            RegistroId = web.RegistroId,
            LoteriaId = cali,
            Numero = "9999",
            Valor = 8_000
        });
        db.RegistrosJuegoManual.Add(web);
        await db.SaveChangesAsync();
        return (new CubrirVentaService(db, new RelojFijo(Ahora)), db, admin);
    }

    private static Loteria Loteria(Guid id, string nombre, DateTime fecha) => new()
    {
        LoteriaId = id,
        Nombre = nombre,
        Estado = EstadoGeneral.Activo,
        Tope = 1_000_000,
        FechaCreacion = fecha
    };

    private sealed class RelojFijo : IClock
    {
        public RelojFijo(DateTime local) => LocalNow = local;
        public DateTime UtcNow => LocalNow;
        public DateTime LocalNow { get; }
    }
}
