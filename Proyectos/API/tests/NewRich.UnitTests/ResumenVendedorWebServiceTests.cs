using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class ResumenVendedorWebServiceTests
{
    // 10:00 a. m. en Colombia.
    private static readonly DateTime AhoraUtc = new(2026, 10, 29, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime AhoraLocal = ZonaHorariaColombia.ALocal(AhoraUtc);
    private static readonly DateOnly Hoy = DateOnly.FromDateTime(AhoraLocal);

    [Fact]
    public async Task Suma_solo_lo_de_hoy_del_vendedor_que_consulta()
    {
        var (sut, db, vendedor, cali, bogota) = await CrearAsync();
        var otro = Guid.NewGuid();
        Individual(db, vendedor, AhoraLocal, cali, "1234", 2000);
        Individual(db, vendedor, AhoraLocal.AddDays(-1), cali, "9999", 5000);
        Individual(db, otro, AhoraLocal, cali, "1234", 7000);
        Combo(db, vendedor, AhoraLocal, 8000, (bogota, "5678", 4000), (bogota, "1111", 4000));
        db.VentasRegistradasWeb.AddRange(
            new VentaRegistradaWeb { VentaRegistradaWebId = Guid.NewGuid(), VendedorId = vendedor, FechaVenta = Hoy, Valor = 50_000, FechaRegistro = AhoraUtc },
            new VentaRegistradaWeb { VentaRegistradaWebId = Guid.NewGuid(), VendedorId = vendedor, FechaVenta = Hoy.AddDays(-1), Valor = 90_000, FechaRegistro = AhoraUtc.AddDays(-1) });
        db.PremiosEntregadosWeb.AddRange(
            Premio(vendedor, AhoraUtc, 120_000),
            Premio(vendedor, AhoraUtc.AddHours(-2), 30_000),
            Premio(vendedor, AhoraUtc.AddDays(-1), 999_000),
            Premio(otro, AhoraUtc, 5_000));
        db.Gastos.AddRange(
            Gasto(vendedor, Hoy, 10_000, EstadoGasto.Activo),
            Gasto(vendedor, Hoy, 2_500, EstadoGasto.Activo),
            Gasto(vendedor, Hoy, 40_000, EstadoGasto.Anulado),
            Gasto(vendedor, Hoy.AddDays(-1), 7_000, EstadoGasto.Activo));
        await db.SaveChangesAsync();

        var resumen = (await sut.ObtenerAsync(vendedor, CancellationToken.None)).Data!;

        resumen.Fecha.Should().Be(Hoy);
        resumen.JuegosRegistrados.Should().Be(2);
        resumen.ApuestasRegistradas.Should().Be(3);
        resumen.ValorJuegos.Should().Be(10_000);
        resumen.VentasRegistradas.Should().Be(1);
        resumen.ValorVentaDelDia.Should().Be(50_000);
        resumen.PremiosEntregados.Should().Be(2);
        resumen.ValorPremiosEntregados.Should().Be(150_000);
        resumen.Gastos.Should().Be(2);
        resumen.ValorGastos.Should().Be(12_500);
    }

    [Fact]
    public async Task Marca_como_ganadoras_las_apuestas_de_hoy_con_el_numero_exacto_del_resultado_de_su_loteria()
    {
        var (sut, db, vendedor, cali, bogota) = await CrearAsync();
        Individual(db, vendedor, AhoraLocal, cali, "1234", 2000);
        Individual(db, vendedor, AhoraLocal, bogota, "1234", 3000);
        Individual(db, vendedor, AhoraLocal, cali, "234", 1000);
        Combo(db, vendedor, AhoraLocal, 8000, (bogota, "5678", 4000), (bogota, "1111", 4000));
        db.NumerosGanadores.AddRange(
            Resultado(cali, AhoraLocal.Date, "1234"),
            Resultado(bogota, AhoraLocal.Date, "5678"),
            Resultado(bogota, AhoraLocal.Date.AddDays(-1), "1111"));
        await db.SaveChangesAsync();

        var ganadores = (await sut.ObtenerAsync(vendedor, CancellationToken.None)).Data!.Ganadores;

        ganadores.Select(g => (g.Loteria, g.Numero, g.Valor, g.Tipo)).Should().BeEquivalentTo(
        [
            ("Cali", "1234", 2000m, TiposRegistroJuego.Individual),
            ("Bogotá", "5678", 4000m, TiposRegistroJuego.Combo)
        ]);
    }

    [Fact]
    public async Task Sin_movimientos_entrega_todo_en_cero()
    {
        var (sut, _, vendedor, _, _) = await CrearAsync();

        var resumen = (await sut.ObtenerAsync(vendedor, CancellationToken.None)).Data!;

        resumen.JuegosRegistrados.Should().Be(0);
        resumen.VentasRegistradas.Should().Be(0);
        resumen.PremiosEntregados.Should().Be(0);
        resumen.Gastos.Should().Be(0);
        resumen.Ganadores.Should().BeEmpty();
    }

    private static void Individual(NewRichDbContext db, Guid usuario, DateTime fecha, Guid loteria, string numero, decimal valor)
    {
        var id = Guid.NewGuid();
        db.RegistrosJuegoManual.Add(new RegistroJuegoManual
        {
            RegistroId = id,
            UsuarioId = usuario,
            NombreCompleto = "Vendedor",
            FechaRegistro = fecha,
            CodigoPublico = Random.Shared.Next(1_000_000, 9_999_999).ToString(),
            IdempotencyKey = Guid.NewGuid().ToString("N"),
            Filas = [new FilaRegistroJuegoManual { FilaId = Guid.NewGuid(), RegistroId = id, LoteriaId = loteria, Numero = numero, Valor = valor }]
        });
    }

    private static void Combo(NewRichDbContext db, Guid usuario, DateTime fecha, decimal total, params (Guid Loteria, string Numero, decimal Valor)[] apuestas)
    {
        var id = Guid.NewGuid();
        db.RegistrosJuegoNumerosWebManual.Add(new RegistroJuegoNumerosWebManual
        {
            RegistroId = id,
            UsuarioId = usuario,
            NombreCompleto = "Vendedor",
            FechaRegistro = fecha,
            CodigoPublico = Random.Shared.Next(1_000_000, 9_999_999).ToString(),
            ValorJugado = total,
            Apuestas = apuestas.Select(a => new ApuestaRegistroJuegoNumerosWebManual
            {
                ApuestaId = Guid.NewGuid(),
                RegistroId = id,
                LoteriaId = a.Loteria,
                Numero = a.Numero,
                Valor = a.Valor
            }).ToList()
        });
    }

    private static PremioEntregadoWeb Premio(Guid vendedor, DateTime registroUtc, decimal valor) => new()
    {
        PremioEntregadoWebId = Guid.NewGuid(),
        VendedorId = vendedor,
        LoteriaId = Guid.NewGuid(),
        Numero = "1234",
        ValorApostado = 1000,
        ValorPremio = valor,
        FechaPremio = DateOnly.FromDateTime(registroUtc),
        FechaRegistro = registroUtc
    };

    private static Gasto Gasto(Guid usuario, DateOnly fecha, decimal valor, EstadoGasto estado) => new()
    {
        GastoId = Guid.NewGuid(),
        TipoGastoId = Guid.NewGuid(),
        FechaGasto = fecha,
        Valor = valor,
        UsuarioRegistroId = usuario,
        Estado = estado,
        ClaveIdempotencia = Guid.NewGuid().ToString("N"),
        FechaCreacion = AhoraUtc
    };

    private static NumeroGanador Resultado(Guid loteria, DateTime fechaJuego, string numero) => new()
    {
        NumeroGanadorId = Guid.NewGuid(),
        LoteriaId = loteria,
        FechaJuego = fechaJuego,
        Numero = numero,
        FechaRegistro = AhoraLocal
    };

    private static async Task<(ResumenVendedorWebService Sut, NewRichDbContext Db, Guid Vendedor, Guid Cali, Guid Bogota)> CrearAsync()
    {
        var db = new NewRichDbContext(new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var cali = new Loteria { LoteriaId = Guid.NewGuid(), Nombre = "Cali", Estado = EstadoGeneral.Activo, FechaCreacion = AhoraLocal };
        var bogota = new Loteria { LoteriaId = Guid.NewGuid(), Nombre = "Bogotá", Estado = EstadoGeneral.Activo, FechaCreacion = AhoraLocal };
        db.Loterias.AddRange(cali, bogota);
        await db.SaveChangesAsync();
        return (new ResumenVendedorWebService(db, new Reloj()), db, Guid.NewGuid(), cali.LoteriaId, bogota.LoteriaId);
    }

    private sealed class Reloj : IClock
    {
        public DateTime UtcNow => AhoraUtc;
        public DateTime LocalNow => AhoraLocal;
    }
}
