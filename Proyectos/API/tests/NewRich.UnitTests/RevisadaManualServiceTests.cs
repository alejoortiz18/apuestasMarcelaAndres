using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class RevisadaManualServiceTests
{
    private static readonly DateTime Ahora = new(2026, 10, 18, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Resumen_muestra_solo_las_apuestas_visibles_de_registrar_juego_y_cubre_esas()
    {
        var (sut, db, admin) = await CreateSutAsync();
        var desde = new DateTime(2026, 10, 18);
        var hasta = desde.AddDays(1).AddTicks(-1);

        var resumen = await sut.ResumenAsync(desde, hasta, 10_000, CancellationToken.None);

        resumen.IsSuccess.Should().BeTrue(resumen.Message);
        var grupo = resumen.Data!.Grupos.Should().ContainSingle().Subject;
        grupo.Loteria.Should().Be("Medellín");
        grupo.Numero.Should().Be("1234");
        grupo.Subtotal.Should().Be(11_000);
        grupo.Lineas.Should().ContainSingle();

        var marcado = await sut.MarcarPorCubrirAsync(admin, [grupo.Lineas[0].Clave], CancellationToken.None);
        marcado.IsSuccess.Should().BeTrue(marcado.Message);
        var otra = await sut.ResumenAsync(desde, hasta, null, CancellationToken.None);
        otra.Data!.Grupos.Should().ContainSingle(g => g.Numero == "5678" && g.Subtotal == 8_000);

        var porCubrir = await sut.PorCubrirAsync(desde, hasta, CancellationToken.None);
        var operacion = porCubrir.Data!.Grupos.Should().ContainSingle().Subject;
        var cubierto = await sut.CubrirAsync(admin, operacion.OperacionId!.Value, 50, CancellationToken.None);
        cubierto.Data!.ComisionValor.Should().Be(5_500);
        cubierto.Data.TotalConComision.Should().Be(16_500);
        (await sut.CubrirAsync(admin, operacion.OperacionId.Value, 50, CancellationToken.None)).Message
            .Should().Be(RevisadaManualMessages.YaProcesado);

        var descuento = await sut.RegistrarDescuentoAsync(admin, operacion.OperacionId.Value, 10, CancellationToken.None);
        descuento.Data!.DescuentoValor.Should().Be(1_650);
        descuento.Data.TotalPagar.Should().Be(14_850);
        (await db.Ventas.CountAsync()).Should().Be(0);
    }

    private static async Task<(RevisadaManualService Sut, NewRichDbContext Db, Guid Admin)> CreateSutAsync()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var admin = Guid.NewGuid();
        var vendedor = Guid.NewGuid();
        var loteria = Guid.NewGuid();
        var fecha = new DateTime(2026, 10, 18, 10, 0, 0);
        db.Usuarios.Add(new Usuario
        {
            UsuarioId = vendedor,
            NombreCompleto = "Vendedor Web",
            NombreUsuario = "vendedorweb",
            Alias = "VW",
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Vendedor,
            Estado = EstadoUsuario.Activo,
            FechaCreacion = fecha
        });
        db.Loterias.Add(new Loteria
        {
            LoteriaId = loteria,
            Nombre = "Medellín",
            Estado = EstadoGeneral.Activo,
            Tope = 1_000_000m,
            FechaCreacion = fecha
        });
        var registro = new RegistroJuegoManual
        {
            RegistroId = Guid.NewGuid(),
            UsuarioId = vendedor,
            NombreCompleto = "Vendedor Web",
            FechaRegistro = fecha,
            CodigoPublico = "1234567",
            IdempotencyKey = "fila"
        };
        registro.Filas.Add(new FilaRegistroJuegoManual
        {
            FilaId = Guid.NewGuid(),
            RegistroId = registro.RegistroId,
            LoteriaId = loteria,
            Numero = "1234",
            Valor = 11_000
        });
        registro.Filas.Add(new FilaRegistroJuegoManual
        {
            FilaId = Guid.NewGuid(),
            RegistroId = registro.RegistroId,
            LoteriaId = loteria,
            Numero = "5678",
            Valor = 8_000
        });
        db.RegistrosJuegoManual.Add(registro);
        var numeros = new RegistroJuegoNumerosWebManual
        {
            RegistroId = Guid.NewGuid(),
            UsuarioId = vendedor,
            NombreCompleto = "Vendedor Web",
            FechaRegistro = fecha,
            CodigoPublico = "7654321",
            ValorJugado = 5_000
        };
        numeros.Apuestas.Add(new ApuestaRegistroJuegoNumerosWebManual
        {
            ApuestaId = Guid.NewGuid(),
            RegistroId = numeros.RegistroId,
            LoteriaId = loteria,
            Numero = "9999",
            Valor = 5_000
        });
        db.RegistrosJuegoNumerosWebManual.Add(numeros);
        await db.SaveChangesAsync();
        return (new RevisadaManualService(db, new RelojFijo(Ahora)), db, admin);
    }

    private sealed class RelojFijo : IClock
    {
        public RelojFijo(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => ZonaHorariaColombia.ALocal(UtcNow);
    }
}
