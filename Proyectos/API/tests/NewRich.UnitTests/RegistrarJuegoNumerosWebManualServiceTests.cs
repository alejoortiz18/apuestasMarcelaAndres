using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.RegistrarJuegoNumerosWebManual;
using NewRich.Application.Services;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class RegistrarJuegoNumerosWebManualServiceTests
{
    private static readonly DateTime Ahora = new(2026, 10, 17, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RegistrarAsync_guarda_numeros_y_loterias_aparte_de_las_ventas_del_pda()
    {
        var (sut, db) = await CreateSutAsync();
        var vendedor = db.Usuarios.Single();
        var loterias = db.Loterias.OrderBy(l => l.Nombre).ToList();

        var result = await sut.RegistrarAsync(
            vendedor.UsuarioId,
            new RegistrarJuegoNumerosWebManualRequest
            {
                RegistroId = Guid.NewGuid(),
                ValorJugado = 10_000,
                Numeros = ["1234", "5678"],
                LoteriaIds = loterias.Select(l => l.LoteriaId).ToList()
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Data!.Numeros.Should().BeEquivalentTo("1234", "5678");
        result.Data.Loterias.Should().BeEquivalentTo("Bogotá", "Cali");
        result.Data.CantidadApuestas.Should().Be(4);
        result.Data.ValorPorApuesta.Should().Be(2_500);
        result.Data.Apuestas.Should().HaveCount(4);
        result.Data.Apuestas.Should().OnlyContain(a => a.Valor == 2_500 && (a.Loteria == "Bogotá" || a.Loteria == "Cali"));
        var registro = await db.RegistrosJuegoNumerosWebManual.Include(r => r.Apuestas).SingleAsync();
        registro.ValorJugado.Should().Be(10_000);
        registro.NombreCompleto.Should().Be(vendedor.NombreCompleto);
        registro.Apuestas.Should().HaveCount(4);
        registro.Apuestas.Select(a => a.Numero).Distinct().Should().BeEquivalentTo("1234", "5678");
        (await db.Ventas.CountAsync()).Should().Be(0);
        (await db.Boletos.CountAsync()).Should().Be(0);
        (await db.Juegos.CountAsync()).Should().Be(0);
        (await db.JuegoLoterias.CountAsync()).Should().Be(0);
        (await db.VentasManual.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task RegistrarAsync_registra_aunque_la_loteria_ya_haya_cerrado()
    {
        var (sut, db) = await CreateSutAsync();
        var vendedor = db.Usuarios.Single();
        var cerrada = new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Chontico",
            Estado = EstadoGeneral.Activo,
            Tope = 1_000_000m,
            HoraInicio = TimeSpan.Zero,
            HoraFin = new TimeSpan(9, 0, 0),
            FechaCreacion = Ahora
        };
        db.Loterias.Add(cerrada);
        db.LoteriasDiasSemana.Add(new LoteriaDiaSemana
        {
            LoteriaId = cerrada.LoteriaId,
            DiaSemana = DiaSemana.Sabado,
            FechaActualizacion = Ahora
        });
        await db.SaveChangesAsync();

        var result = await sut.RegistrarAsync(
            vendedor.UsuarioId,
            new RegistrarJuegoNumerosWebManualRequest
            {
                RegistroId = Guid.NewGuid(),
                ValorJugado = 2_800,
                Numeros = ["6363"],
                LoteriaIds = [cerrada.LoteriaId]
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Data!.Apuestas.Should().ContainSingle(a => a.Loteria == "Chontico" && a.Numero == "6363" && a.Valor == 2_800);
    }

    private static async Task<(RegistrarJuegoNumerosWebManualService Sut, NewRichDbContext Db)> CreateSutAsync()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var vendedorId = Guid.NewGuid();
        db.Usuarios.Add(new Usuario
        {
            UsuarioId = vendedorId,
            NombreCompleto = "Vendedor Web",
            NombreUsuario = "vendedorweb",
            Alias = "VW",
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Vendedor,
            Estado = EstadoUsuario.Activo,
            FechaCreacion = Ahora
        });
        foreach (var nombre in new[] { "Cali", "Bogotá" })
        {
            var loteriaId = Guid.NewGuid();
            db.Loterias.Add(new Loteria
            {
                LoteriaId = loteriaId,
                Nombre = nombre,
                Estado = EstadoGeneral.Activo,
                Tope = 1_000_000m,
                HoraInicio = TimeSpan.Zero,
                HoraFin = new TimeSpan(23, 59, 0),
                FechaCreacion = Ahora
            });
            db.LoteriasDiasSemana.Add(new LoteriaDiaSemana
            {
                LoteriaId = loteriaId,
                DiaSemana = DiaSemana.Sabado,
                FechaActualizacion = Ahora
            });
        }

        await db.SaveChangesAsync();
        var sut = new RegistrarJuegoNumerosWebManualService(db, new RegistrarJuegoNumerosWebManualRepositorio(db), new RelojFijo(Ahora));
        return (sut, db);
    }

    private sealed class RelojFijo : IClock
    {
        public RelojFijo(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => ZonaHorariaColombia.ALocal(UtcNow);
    }
}
