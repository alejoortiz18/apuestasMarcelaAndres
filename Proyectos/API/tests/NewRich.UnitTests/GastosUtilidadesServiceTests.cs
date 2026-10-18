using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Contracts.Utilidades;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class GastosUtilidadesServiceTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 18);

    [Fact]
    public async Task Registrar_guarda_el_gasto_del_usuario_y_no_lo_duplica()
    {
        await using var db = Nueva();
        var sut = new GastosUtilidadesService(db);
        var usuario = Guid.NewGuid();
        var otro = Guid.NewGuid();
        var transporte = await TipoTransporte(sut);

        var guardado = await sut.RegistrarAsync(
            usuario,
            new RegistrarGastoRequest(transporte, Hoy, 15_000, "Bus", "clave-1"),
            CancellationToken.None);

        guardado.IsSuccess.Should().BeTrue(guardado.Message);
        guardado.Data!.UsuarioRegistroId.Should().Be(usuario);
        guardado.Data.Estado.Should().Be(nameof(EstadoGasto.Activo));

        var repetido = await sut.RegistrarAsync(
            usuario,
            new RegistrarGastoRequest(transporte, Hoy, 99_000, "Otro", "clave-1"),
            CancellationToken.None);

        repetido.IsSuccess.Should().BeTrue();
        repetido.Data!.Valor.Should().Be(15_000);
        (await db.Gastos.CountAsync()).Should().Be(1);

        await sut.RegistrarAsync(
            otro,
            new RegistrarGastoRequest(transporte, Hoy, 9_000, "Taxi", "clave-2"),
            CancellationToken.None);

        var mios = await sut.ConsultarPropiosAsync(usuario, "hoy", null, null, Hoy, 1, 10, CancellationToken.None);
        mios.Data!.TotalValor.Should().Be(15_000);
        mios.Data.Cantidad.Should().Be(1);
        mios.Data.Items.Should().ContainSingle(i => i.Valor == 15_000 && i.Observacion == "Bus");
    }

    [Fact]
    public async Task Registrar_rechaza_valor_invalido_tipo_inactivo_y_observacion_vacia()
    {
        await using var db = Nueva();
        var sut = new GastosUtilidadesService(db);
        var usuario = Guid.NewGuid();
        var transporte = await TipoTransporte(sut);

        (await sut.RegistrarAsync(usuario, new RegistrarGastoRequest(transporte, Hoy, 0, "Bus", "k1"), CancellationToken.None))
            .Message.Should().Be(UtilidadesMessages.ValorInvalido);
        (await sut.RegistrarAsync(usuario, new RegistrarGastoRequest(transporte, Hoy, 10.129m, "Bus", "k2"), CancellationToken.None))
            .Message.Should().Be(UtilidadesMessages.ValorInvalido);
        (await sut.RegistrarAsync(usuario, new RegistrarGastoRequest(transporte, Hoy, 10, "  ", "k3"), CancellationToken.None))
            .Message.Should().Be(UtilidadesMessages.ObservacionObligatoria);

        var tipo = await db.TiposGasto.SingleAsync(t => t.TipoGastoId == transporte);
        tipo.Activo = false;
        await db.SaveChangesAsync();

        (await sut.RegistrarAsync(usuario, new RegistrarGastoRequest(transporte, Hoy, 10, "Bus", "k4"), CancellationToken.None))
            .Message.Should().Be(UtilidadesMessages.TipoNoDisponible);
        (await db.Gastos.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Consultar_propios_no_suma_anulados_y_respeta_el_periodo()
    {
        await using var db = Nueva();
        var sut = new GastosUtilidadesService(db);
        var usuario = Guid.NewGuid();
        var transporte = await TipoTransporte(sut);

        await sut.RegistrarAsync(usuario, new RegistrarGastoRequest(transporte, new DateOnly(2026, 9, 30), 4_000, "Septiembre", "s1"), CancellationToken.None);
        await sut.RegistrarAsync(usuario, new RegistrarGastoRequest(transporte, new DateOnly(2026, 10, 1), 5_000, "Octubre", "s2"), CancellationToken.None);
        var anulado = await sut.RegistrarAsync(usuario, new RegistrarGastoRequest(transporte, Hoy, 7_000, "Hoy", "s3"), CancellationToken.None);
        var fila = await db.Gastos.SingleAsync(g => g.GastoId == anulado.Data!.GastoId);
        fila.Estado = EstadoGasto.Anulado;
        await db.SaveChangesAsync();

        var mesAnterior = await sut.ConsultarPropiosAsync(usuario, "mes-anterior", null, null, Hoy, 1, 10, CancellationToken.None);
        mesAnterior.Data!.Desde.Should().Be(new DateOnly(2026, 9, 1));
        mesAnterior.Data.Hasta.Should().Be(new DateOnly(2026, 9, 30));
        mesAnterior.Data.TotalValor.Should().Be(4_000);
        mesAnterior.Data.Cantidad.Should().Be(1);

        var hoy = await sut.ConsultarPropiosAsync(usuario, "hoy", null, null, Hoy, 1, 10, CancellationToken.None);
        hoy.Data!.TotalValor.Should().Be(0);
        hoy.Data.Cantidad.Should().Be(0);
        hoy.Data.TotalFilas.Should().Be(1);
        hoy.Data.Items.Single().Estado.Should().Be(nameof(EstadoGasto.Anulado));
    }

    [Fact]
    public async Task Informe_reparte_por_tipo_y_por_dia_y_deja_fuera_los_anulados()
    {
        await using var db = Nueva();
        var sut = new GastosUtilidadesService(db);
        var usuario = Guid.NewGuid();
        var tipos = await sut.ListarTiposActivosAsync(CancellationToken.None);
        var transporte = tipos.Data!.Single(t => t.Nombre == "Transporte").Id;
        var alimentacion = tipos.Data!.Single(t => t.Nombre == "Alimentación").Id;

        await sut.RegistrarAsync(usuario, new RegistrarGastoRequest(transporte, Hoy, 15_000, "Bus", "i1"), CancellationToken.None);
        await sut.RegistrarAsync(usuario, new RegistrarGastoRequest(alimentacion, Hoy.AddDays(-1), 5_000, "Almuerzo", "i2"), CancellationToken.None);
        var anulado = await sut.RegistrarAsync(usuario, new RegistrarGastoRequest(transporte, Hoy, 7_000, "No va", "i3"), CancellationToken.None);
        var fila = await db.Gastos.SingleAsync(g => g.GastoId == anulado.Data!.GastoId);
        fila.Estado = EstadoGasto.Anulado;
        await db.SaveChangesAsync();

        var informe = await sut.ConsultarInformePropioAsync(usuario, "ultimos-7", null, null, Hoy, CancellationToken.None);

        informe.IsSuccess.Should().BeTrue(informe.Message);
        informe.Data!.TotalValor.Should().Be(20_000);
        informe.Data.Cantidad.Should().Be(2);
        informe.Data.Promedio.Should().Be(10_000);
        informe.Data.MayorValor.Should().Be(15_000);
        informe.Data.MayorTipo.Should().Be("Transporte");
        informe.Data.AnuladosCantidad.Should().Be(1);
        informe.Data.AnuladosValor.Should().Be(7_000);
        informe.Data.PorTipo.Select(t => (t.Tipo, t.Valor, t.Cantidad)).Should().Equal(
            ("Transporte", 15_000m, 1),
            ("Alimentación", 5_000m, 1));
        informe.Data.PorDia.Select(d => (d.Fecha, d.Valor)).Should().Equal(
            (Hoy.AddDays(-1), 5_000m),
            (Hoy, 15_000m));
    }

    [Fact]
    public async Task Resumen_suma_los_gastos_de_todos_y_deja_ingresos_en_blanco()
    {
        await using var db = Nueva();
        var sut = new GastosUtilidadesService(db);
        var uno = Guid.NewGuid();
        var dos = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var transporte = await TipoTransporte(sut);
        var alimentacion = (await sut.ListarTiposActivosAsync(CancellationToken.None)).Data!.Single(t => t.Nombre == "Alimentación").Id;
        await sut.RegistrarAsync(uno, new RegistrarGastoRequest(transporte, Hoy, 15_000, "Bus", "a1"), CancellationToken.None);
        await sut.RegistrarAsync(dos, new RegistrarGastoRequest(alimentacion, Hoy, 5_000, "Almuerzo", "a2"), CancellationToken.None);
        var anulado = await sut.RegistrarAsync(uno, new RegistrarGastoRequest(transporte, Hoy, 7_000, "Taxi", "a3"), CancellationToken.None);
        await sut.AnularAsync(admin, anulado.Data!.GastoId, new AnularGastoRequest("Duplicado"), CancellationToken.None);

        var resumen = await sut.ConsultarResumenAsync("hoy", null, null, Hoy, CancellationToken.None);

        resumen.IsSuccess.Should().BeTrue(resumen.Message);
        resumen.Data!.Gastos.Should().Be(20_000);
        resumen.Data.Cantidad.Should().Be(2);
        resumen.Data.Ingresos.Should().BeNull();
        resumen.Data.Ganancia.Should().BeNull();
        resumen.Data.PorTipo.Select(t => (t.Tipo, t.Valor)).Should().Equal(
            ("Transporte", 15_000m),
            ("Alimentación", 5_000m));

        var informe = await sut.ConsultarInformeAsync("hoy", null, null, Hoy, CancellationToken.None);
        informe.Data!.TotalValor.Should().Be(20_000);
        informe.Data.Cantidad.Should().Be(2);
        informe.Data.AnuladosValor.Should().Be(7_000);
        var propio = await sut.ConsultarInformePropioAsync(dos, "hoy", null, null, Hoy, CancellationToken.None);
        propio.Data!.TotalValor.Should().Be(5_000);
    }

    [Fact]
    public async Task Corregir_cambia_valor_fecha_y_tipo_con_motivo_y_deja_la_observacion()
    {
        await using var db = Nueva();
        var sut = new GastosUtilidadesService(db);
        var usuario = Guid.NewGuid();
        var admin = Guid.NewGuid();
        var transporte = await TipoTransporte(sut);
        var alimentacion = (await sut.ListarTiposActivosAsync(CancellationToken.None)).Data!.Single(t => t.Nombre == "Alimentación").Id;
        var guardado = await sut.RegistrarAsync(usuario, new RegistrarGastoRequest(transporte, Hoy, 15_000, "Bus", "c1"), CancellationToken.None);

        (await sut.CorregirAsync(admin, guardado.Data!.GastoId, new CorregirGastoRequest(12_000, Hoy.AddDays(-1), alimentacion, "  "), CancellationToken.None))
            .Message.Should().Be(UtilidadesMessages.MotivoObligatorio);

        var corregido = await sut.CorregirAsync(
            admin,
            guardado.Data.GastoId,
            new CorregirGastoRequest(12_000, Hoy.AddDays(-1), alimentacion, "Valor mal digitado"),
            CancellationToken.None);

        corregido.IsSuccess.Should().BeTrue(corregido.Message);
        corregido.Data!.Valor.Should().Be(12_000);
        corregido.Data.FechaGasto.Should().Be(Hoy.AddDays(-1));
        corregido.Data.Tipo.Should().Be("Alimentación");
        corregido.Data.Observacion.Should().Be("Bus");
        corregido.Data.UsuarioRegistroId.Should().Be(usuario);
        var rastro = await db.AuditoriaCambiosGasto.SingleAsync();
        rastro.Accion.Should().Be("Edicion");
        rastro.ValorAnterior.Should().Be(15_000);
        rastro.ValorNuevo.Should().Be(12_000);
        rastro.Motivo.Should().Be("Valor mal digitado");
        rastro.UsuarioId.Should().Be(admin);
    }

    [Fact]
    public async Task Catalogo_rechaza_un_nombre_que_ya_existe()
    {
        await using var db = Nueva();
        var sut = new GastosUtilidadesService(db);
        var admin = Guid.NewGuid();
        await sut.ListarTiposActivosAsync(CancellationToken.None);

        var repetido = await sut.CrearTipoAsync(admin, new GuardarTipoGastoRequest("  Transporte  ", null, true), CancellationToken.None);

        repetido.IsSuccess.Should().BeFalse();
        repetido.Message.Should().Be(UtilidadesMessages.TipoDuplicado);
    }

    private static NewRichDbContext Nueva()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NewRichDbContext(options);
    }

    private static async Task<Guid> TipoTransporte(GastosUtilidadesService sut)
    {
        var tipos = await sut.ListarTiposActivosAsync(CancellationToken.None);
        return tipos.Data!.Single(t => t.Nombre == "Transporte").Id;
    }
}
