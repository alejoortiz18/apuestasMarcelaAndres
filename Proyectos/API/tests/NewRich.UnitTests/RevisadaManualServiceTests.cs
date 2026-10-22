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

    [Fact]
    public async Task Cubrir_desde_resumen_aplica_la_comision_sin_pasar_por_por_cubrir()
    {
        var (sut, db, admin) = await CreateSutAsync();
        var desde = new DateTime(2026, 10, 18);
        var hasta = desde.AddDays(1).AddTicks(-1);
        var grupo = (await sut.ResumenAsync(desde, hasta, null, CancellationToken.None)).Data!.Grupos.Single(g => g.Numero == "1234");

        var cubierto = await sut.CubrirDesdeResumenAsync(admin, grupo.Lineas.Select(l => l.Clave).ToList(), 50, 10, CancellationToken.None);

        cubierto.IsSuccess.Should().BeTrue(cubierto.Message);
        cubierto.Message.Should().Be(RevisadaManualMessages.Cubierto);
        cubierto.Data!.Subtotal.Should().Be(11_000);
        cubierto.Data.ComisionValor.Should().Be(5_500);
        cubierto.Data.TotalConComision.Should().Be(16_500);
        cubierto.Data.DescuentoPorcentaje.Should().Be(10);
        cubierto.Data.DescuentoValor.Should().Be(1_650);
        cubierto.Data.TotalPagar.Should().Be(14_850);
        (await sut.PorCubrirAsync(desde, hasta, CancellationToken.None)).Data!.Grupos.Should().BeEmpty();
        (await sut.CubiertoAsync(desde, hasta, CancellationToken.None)).Data!.Grupos.Should().ContainSingle(g => g.Numero == "1234" && g.TotalPagar == 14_850);
        (await sut.ResumenAsync(desde, hasta, null, CancellationToken.None)).Data!.Grupos.Should().NotContain(g => g.Numero == "1234");
        (await sut.CubrirDesdeResumenAsync(admin, grupo.Lineas.Select(l => l.Clave).ToList(), 50, 10, CancellationToken.None)).Message
            .Should().Be(RevisadaManualMessages.YaProcesado);
    }

    [Fact]
    public async Task Cubrir_desde_resumen_con_porcentaje_invalido_no_toma_los_registros()
    {
        var (sut, db, admin) = await CreateSutAsync();
        var desde = new DateTime(2026, 10, 18);
        var hasta = desde.AddDays(1).AddTicks(-1);
        var grupo = (await sut.ResumenAsync(desde, hasta, null, CancellationToken.None)).Data!.Grupos.Single(g => g.Numero == "1234");

        var result = await sut.CubrirDesdeResumenAsync(admin, grupo.Lineas.Select(l => l.Clave).ToList(), 50, 101, CancellationToken.None);

        result.Message.Should().Be(RevisadaManualMessages.PorcentajeInvalido);
        (await db.OperacionesRevisadaManual.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Recubrir_cubre_solo_lo_pendiente_de_numeros_y_loterias_cubiertos_hoy()
    {
        var (sut, db, admin) = await CreateSutAsync();
        var desde = new DateTime(2026, 10, 18);
        var hasta = desde.AddDays(1).AddTicks(-1);
        var grupo = (await sut.ResumenAsync(desde, hasta, null, CancellationToken.None)).Data!.Grupos.Single(g => g.Numero == "1234");
        await sut.CubrirDesdeResumenAsync(admin, grupo.Lineas.Select(l => l.Clave).ToList(), 20, 10, CancellationToken.None);
        await AgregarFilaAsync(db, "1234", 2_000);

        var previa = await sut.RecubrirPreviaAsync(desde, hasta, null, CancellationToken.None);

        previa.IsSuccess.Should().BeTrue(previa.Message);
        var nuevo = previa.Data!.Grupos.Should().ContainSingle().Subject;
        nuevo.Numero.Should().Be("1234");
        nuevo.Subtotal.Should().Be(2_000);

        var recubierto = await sut.RecubrirAsync(admin, nuevo.Lineas.Select(l => l.Clave).ToList(), 20, 10, CancellationToken.None);

        recubierto.IsSuccess.Should().BeTrue(recubierto.Message);
        recubierto.Message.Should().Be(RevisadaManualMessages.Recubierto);
        var operacion = recubierto.Data!.Grupos.Should().ContainSingle().Subject;
        operacion.TotalConComision.Should().Be(2_400);
        operacion.TotalPagar.Should().Be(2_160);
        (await sut.CubiertoAsync(desde, hasta, CancellationToken.None)).Data!.Grupos.Count(g => g.Numero == "1234").Should().Be(2);
        (await sut.RecubrirPreviaAsync(desde, hasta, null, CancellationToken.None)).Data!.Grupos.Should().BeEmpty();
    }

    [Fact]
    public async Task Recubrir_rechaza_numeros_que_no_se_cubrieron_hoy()
    {
        var (sut, _, admin) = await CreateSutAsync();
        var desde = new DateTime(2026, 10, 18);
        var hasta = desde.AddDays(1).AddTicks(-1);
        var otro = (await sut.ResumenAsync(desde, hasta, null, CancellationToken.None)).Data!.Grupos.Single(g => g.Numero == "5678");

        var result = await sut.RecubrirAsync(admin, otro.Lineas.Select(l => l.Clave).ToList(), 20, 10, CancellationToken.None);

        result.Message.Should().Be(RevisadaManualMessages.NoCubiertoHoy);
    }

    [Fact]
    public async Task Historico_agrupa_por_fecha_de_cubrimiento_loteria_y_numero()
    {
        var (sut, db, admin) = await CreateSutAsync();
        var desde = new DateTime(2026, 10, 18);
        var hasta = desde.AddDays(1).AddTicks(-1);
        var resumen = (await sut.ResumenAsync(desde, hasta, null, CancellationToken.None)).Data!.Grupos;
        await sut.CubrirDesdeResumenAsync(admin, resumen.Single(g => g.Numero == "1234").Lineas.Select(l => l.Clave).ToList(), 20, 10, CancellationToken.None);
        await sut.CubrirDesdeResumenAsync(admin, resumen.Single(g => g.Numero == "5678").Lineas.Select(l => l.Clave).ToList(), 20, 10, CancellationToken.None);
        await AgregarFilaAsync(db, "1234", 2_000);
        var nuevo = (await sut.RecubrirPreviaAsync(desde, hasta, null, CancellationToken.None)).Data!.Grupos.Single();
        await sut.RecubrirAsync(admin, nuevo.Lineas.Select(l => l.Clave).ToList(), 20, 10, CancellationToken.None);
        var ayer = await db.OperacionesRevisadaManual.FirstAsync(o => o.Numero == "5678");
        ayer.FechaOperacion = ayer.FechaOperacion.AddDays(-1);
        await db.SaveChangesAsync();

        var historico = await sut.CubiertoHistoricoAsync(desde.AddDays(-1), desde, "subtotal", true, 1, 20, CancellationToken.None);

        historico.IsSuccess.Should().BeTrue(historico.Message);
        var datos = historico.Data!;
        datos.TotalFilas.Should().Be(2);
        datos.Filas.Select(f => (f.Numero, f.Subtotal, f.Registros)).Should().Equal(("1234", 13_000m, 2), ("5678", 8_000m, 1));
        datos.Filas[0].Cubrimientos.Should().HaveCount(2);
        datos.Filas[0].TotalPagar.Should().Be(14_040m);
        datos.Filas[1].Fecha.Should().Be(desde.AddDays(-1));
        datos.Subtotal.Should().Be(21_000m);
        (await sut.CubiertoHoyAsync(CancellationToken.None)).Data!.Grupos.Should().OnlyContain(g => g.Numero == "1234");
    }

    [Fact]
    public async Task Historico_carga_por_paginas()
    {
        var (sut, _, admin) = await CreateSutAsync();
        var desde = new DateTime(2026, 10, 18);
        var hasta = desde.AddDays(1).AddTicks(-1);
        foreach (var grupo in (await sut.ResumenAsync(desde, hasta, null, CancellationToken.None)).Data!.Grupos)
        {
            await sut.CubrirDesdeResumenAsync(admin, grupo.Lineas.Select(l => l.Clave).ToList(), 20, 10, CancellationToken.None);
        }

        var primera = await sut.CubiertoHistoricoAsync(desde, desde, "numero", false, 1, 2, CancellationToken.None);
        var segunda = await sut.CubiertoHistoricoAsync(desde, desde, "numero", false, 2, 2, CancellationToken.None);

        primera.Data!.TotalFilas.Should().Be(3);
        primera.Data.HayMas.Should().BeTrue();
        primera.Data.Filas.Select(f => f.Numero).Should().Equal("1234", "5678");
        segunda.Data!.HayMas.Should().BeFalse();
        segunda.Data.Filas.Select(f => f.Numero).Should().Equal("9999");
    }

    [Fact]
    public async Task Historico_de_resumen_muestra_pendientes_y_cubiertos_por_fecha_loteria_y_numero()
    {
        var (sut, db, admin) = await CreateSutAsync();
        var desde = new DateTime(2026, 10, 18);
        var hasta = desde.AddDays(1).AddTicks(-1);
        var grupo = (await sut.ResumenAsync(desde, hasta, null, CancellationToken.None)).Data!.Grupos.Single(g => g.Numero == "1234");
        await sut.CubrirDesdeResumenAsync(admin, grupo.Lineas.Select(l => l.Clave).ToList(), 20, 10, CancellationToken.None);
        await AgregarFilaAsync(db, "1234", 2_000);

        var historico = await sut.ResumenHistoricoAsync(desde, desde, "estado", false, 1, 20, CancellationToken.None);

        historico.IsSuccess.Should().BeTrue(historico.Message);
        var datos = historico.Data!;
        datos.TotalFilas.Should().Be(4);
        datos.Filas.Should().Contain(f => f.Numero == "1234" && f.Estado == "Cubierto" && f.Subtotal == 11_000 && f.Registros == 1);
        datos.Filas.Should().Contain(f => f.Numero == "1234" && f.Estado == "Pendiente" && f.Subtotal == 2_000);
        datos.Filas.Should().Contain(f => f.Numero == "5678" && f.Estado == "Pendiente");
        datos.Filas[0].Estado.Should().Be("Cubierto");
        datos.SubtotalCubierto.Should().Be(11_000);
        datos.SubtotalPendiente.Should().Be(15_000);
        datos.Subtotal.Should().Be(26_000);
    }

    private static async Task AgregarFilaAsync(NewRichDbContext db, string numero, decimal valor)
    {
        var existente = await db.RegistrosJuegoManual.AsNoTracking().FirstAsync();
        var loteriaId = await db.Loterias.Select(l => l.LoteriaId).FirstAsync();
        var registro = new RegistroJuegoManual
        {
            RegistroId = Guid.NewGuid(),
            UsuarioId = existente.UsuarioId,
            NombreCompleto = existente.NombreCompleto,
            FechaRegistro = existente.FechaRegistro.AddHours(1),
            CodigoPublico = "1111111",
            IdempotencyKey = Guid.NewGuid().ToString("N")
        };
        registro.Filas.Add(new FilaRegistroJuegoManual
        {
            FilaId = Guid.NewGuid(),
            RegistroId = registro.RegistroId,
            LoteriaId = loteriaId,
            Numero = numero,
            Valor = valor
        });
        db.RegistrosJuegoManual.Add(registro);
        await db.SaveChangesAsync();
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
