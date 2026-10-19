using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;
using NewRich.Domain.Entities;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class RetencionJugadaManualServiceTests
{
    private static readonly DateTime AntesDelCierre = new(2026, 10, 18, 23, 58, 0);
    private static readonly DateTime Cierre = new(2026, 10, 18, 23, 59, 0);

    [Fact]
    public async Task Conserva_la_jugada_del_17_hasta_un_minuto_antes_del_cierre_del_18()
    {
        await using var db = Nueva();
        var fila = await JugadaPorFila(db, new DateTime(2026, 10, 17, 8, 0, 0), "1111111");

        await new RetencionJugadaManualService(db, new Reloj(AntesDelCierre)).EjecutarAsync(CancellationToken.None);

        (await db.RegistrosJuegoManual.AnyAsync(r => r.RegistroId == fila)).Should().BeTrue();
    }

    [Fact]
    public async Task Borra_la_jugada_del_17_a_las_1159_del_18_aunque_haya_entrado_tarde_y_este_cubierta()
    {
        await using var db = Nueva();
        var temprana = await JugadaPorFila(db, new DateTime(2026, 10, 17, 0, 5, 0), "2222222");
        var tardia = await JugadaNumeros(db, new DateTime(2026, 10, 17, 23, 40, 0), "3333333");
        var reciente = await JugadaPorFila(db, new DateTime(2026, 10, 18, 9, 0, 0), "4444444");
        var filaTardia = await db.RegistrosJuegoNumerosWebManual.Include(r => r.Apuestas).SingleAsync(r => r.RegistroId == tardia);
        var operacion = OperacionCubierta(filaTardia.Apuestas.Single().ApuestaId, 5_000, new DateTime(2026, 10, 17, 23, 40, 0));
        var parametro = Guid.NewGuid();
        db.ParametrosRevisadaManual.Add(new ParametroRevisadaManual { ParametroId = parametro, ComisionPorcentaje = 10 });
        db.OperacionesRevisadaManual.Add(operacion);
        db.Ventas.Add(new Venta { VentaId = Guid.NewGuid(), FechaVenta = new DateTime(2026, 10, 16, 10, 0, 0), Total = 1_000 });
        await db.SaveChangesAsync();

        await new RetencionJugadaManualService(db, new Reloj(Cierre)).EjecutarAsync(CancellationToken.None);

        (await db.RegistrosJuegoManual.AnyAsync(r => r.RegistroId == temprana)).Should().BeFalse();
        (await db.RegistrosJuegoNumerosWebManual.AnyAsync(r => r.RegistroId == tardia)).Should().BeFalse();
        (await db.Set<ApuestaRegistroJuegoNumerosWebManual>().AnyAsync()).Should().BeFalse();
        (await db.OperacionesRevisadaManual.AnyAsync()).Should().BeFalse();
        (await db.LineasRevisadaManual.AnyAsync()).Should().BeFalse();
        (await db.RegistrosJuegoManual.AnyAsync(r => r.RegistroId == reciente)).Should().BeTrue();
        (await db.ParametrosRevisadaManual.AnyAsync(p => p.ParametroId == parametro)).Should().BeTrue();
        (await db.Ventas.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task En_un_grupo_mixto_borra_solo_la_jugada_vencida_y_ajusta_el_subtotal()
    {
        await using var db = Nueva();
        var vieja = await JugadaPorFila(db, new DateTime(2026, 10, 17, 12, 0, 0), "5555555");
        var nueva = await JugadaPorFila(db, new DateTime(2026, 10, 18, 12, 0, 0), "6666666");
        var filaVieja = await db.Set<FilaRegistroJuegoManual>().SingleAsync(f => f.RegistroId == vieja);
        var filaNueva = await db.Set<FilaRegistroJuegoManual>().SingleAsync(f => f.RegistroId == nueva);
        var operacion = new OperacionRevisadaManual
        {
            OperacionId = Guid.NewGuid(),
            Estado = OperacionRevisadaManual.EstadoPorCubrir,
            Numero = "1234",
            Subtotal = 7_000,
            Lineas =
            [
                Linea(LineaRevisadaManual.OrigenFila, filaVieja.FilaId, 4_000, new DateTime(2026, 10, 17, 12, 0, 0)),
                Linea(LineaRevisadaManual.OrigenFila, filaNueva.FilaId, 3_000, new DateTime(2026, 10, 18, 12, 0, 0))
            ]
        };
        db.OperacionesRevisadaManual.Add(operacion);
        await db.SaveChangesAsync();

        await new RetencionJugadaManualService(db, new Reloj(Cierre)).EjecutarAsync(CancellationToken.None);

        var queda = await db.OperacionesRevisadaManual.Include(o => o.Lineas).SingleAsync();
        queda.Lineas.Should().ContainSingle(l => l.OrigenId == filaNueva.FilaId);
        queda.Subtotal.Should().Be(3_000);
        (await db.RegistrosJuegoManual.AnyAsync(r => r.RegistroId == vieja)).Should().BeFalse();
        (await db.RegistrosJuegoManual.AnyAsync(r => r.RegistroId == nueva)).Should().BeTrue();
    }

    private static async Task<Guid> JugadaPorFila(NewRichDbContext db, DateTime fecha, string codigo)
    {
        var id = Guid.NewGuid();
        var registro = new RegistroJuegoManual
        {
            RegistroId = id,
            UsuarioId = Guid.NewGuid(),
            FechaRegistro = fecha,
            CodigoPublico = codigo,
            IdempotencyKey = codigo,
            Filas = [new FilaRegistroJuegoManual { FilaId = Guid.NewGuid(), RegistroId = id, Numero = "1234", Valor = 4_000 }]
        };
        db.RegistrosJuegoManual.Add(registro);
        await db.SaveChangesAsync();
        return id;
    }

    private static async Task<Guid> JugadaNumeros(NewRichDbContext db, DateTime fecha, string codigo)
    {
        var id = Guid.NewGuid();
        db.RegistrosJuegoNumerosWebManual.Add(new RegistroJuegoNumerosWebManual
        {
            RegistroId = id,
            UsuarioId = Guid.NewGuid(),
            FechaRegistro = fecha,
            CodigoPublico = codigo,
            ValorJugado = 5_000,
            Apuestas = [new ApuestaRegistroJuegoNumerosWebManual { ApuestaId = Guid.NewGuid(), RegistroId = id, Numero = "9999", Valor = 5_000 }]
        });
        await db.SaveChangesAsync();
        return id;
    }

    private static OperacionRevisadaManual OperacionCubierta(Guid apuestaId, decimal valor, DateTime fecha) =>
        new()
        {
            OperacionId = Guid.NewGuid(),
            Estado = OperacionRevisadaManual.EstadoCubierto,
            Numero = "9999",
            Subtotal = valor,
            ComisionPorcentaje = 10,
            ComisionValor = 500,
            TotalConComision = valor + 500,
            Lineas = [Linea(LineaRevisadaManual.OrigenNumeros, apuestaId, valor, fecha)]
        };

    private static LineaRevisadaManual Linea(string origen, Guid origenId, decimal valor, DateTime fecha) =>
        new()
        {
            LineaId = Guid.NewGuid(),
            Origen = origen,
            OrigenId = origenId,
            Valor = valor,
            FechaRegistro = fecha
        };

    private static NewRichDbContext Nueva()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NewRichDbContext(options);
    }

    private sealed class Reloj(DateTime local) : IClock
    {
        public DateTime UtcNow => local;
        public DateTime LocalNow => local;
    }
}
