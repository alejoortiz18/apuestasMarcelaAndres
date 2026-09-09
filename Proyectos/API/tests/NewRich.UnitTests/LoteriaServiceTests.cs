using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;
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
