using FluentAssertions;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Admin.Services;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Contracts.RevisadaManual;
using NewRich.Application.Contracts.Utilidades;

namespace NewRich.Admin.Tests;

public sealed class VentasInformeTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 20);

    [Theory]
    [InlineData("hoy", "2026-10-20")]
    [InlineData("7", "2026-10-14")]
    [InlineData("15", "2026-10-06")]
    [InlineData("30", "2026-09-21")]
    public void Los_periodos_relativos_terminan_hoy(string periodo, string inicio)
    {
        var (desde, hasta) = VentasInforme.Rango(new FiltrosPeriodoInforme { Periodo = periodo }, Hoy);

        desde.Should().Be(DateOnly.Parse(inicio));
        hasta.Should().Be(Hoy);
    }

    [Fact]
    public void El_rango_admite_maximo_31_dias()
    {
        var valido = new FiltrosPeriodoInforme { Periodo = "rango", Desde = new DateOnly(2026, 9, 1), Hasta = new DateOnly(2026, 10, 1) };
        var largo = new FiltrosPeriodoInforme { Periodo = "rango", Desde = new DateOnly(2026, 9, 1), Hasta = new DateOnly(2026, 10, 2) };
        var invertido = new FiltrosPeriodoInforme { Periodo = "rango", Desde = new DateOnly(2026, 10, 2), Hasta = new DateOnly(2026, 10, 1) };

        VentasInforme.Validar(valido).Should().BeNull();
        VentasInforme.Validar(largo).Should().Be(UiTexts.InformeRangoMaximo);
        VentasInforme.Validar(invertido).Should().Be(UiTexts.InformeRangoInvalido);
        VentasInforme.Validar(new FiltrosPeriodoInforme { Periodo = "rango" }).Should().Be(UiTexts.InformeRangoIncompleto);
        VentasInforme.Validar(new FiltrosPeriodoInforme { Periodo = "7", Grupos = [] }).Should().Be(UiTexts.InformeEligeGrupo);
        VentasInforme.Validar(new FiltrosPeriodoInforme { Periodo = "7", Vendedores = [] }).Should().Be(UiTexts.InformeEligeVendedor);
        VentasInforme.Validar(new FiltrosPeriodoInforme { Periodo = "7", TiposEgreso = [] }).Should().Be(UiTexts.InformeEligeTipoEgreso);
    }

    [Fact]
    public void Egresos_solo_suma_y_muestra_los_tipos_elegidos()
    {
        var fuente = new FuenteInformeVentas
        {
            Kpi = new KpiResponse { ValorPremiosEntregados = 100m },
            PremiosWeb = [Premio("Cali", 50m)],
            Gastos = Gastos(300m),
            Cubierto = new VistaRevisadaManualResponse { Total = 700m, Jugadas = 2 }
        };

        var informe = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "hoy", TiposEgreso = [VentasInforme.TipoEgresoGastos, VentasInforme.TipoEgresoCubierto] }, [], Hoy, Hoy, DateTime.Now, 1, 10);

        informe.Resumen.TotalEgresos.Should().Be(1_000m);
        informe.Egresos.Conceptos.Select(c => c.Texto).Should().Equal(UiTexts.InformeConceptoCubierto, UiTexts.InformeConceptoGastos);
        informe.Egresos.IncluyePremios.Should().BeFalse();
        informe.Egresos.IncluyeGastos.Should().BeTrue();
        informe.Egresos.PremiosWeb.TotalCount.Should().Be(0);
        informe.Egresos.PremiosPorLoteria.Should().BeEmpty();
    }

    [Fact]
    public void Los_premios_por_loteria_suman_el_valor_de_cada_loteria()
    {
        var fuente = new FuenteInformeVentas { PremiosWeb = [Premio("Cali", 50m), Premio("Bogotá", 120m), Premio("Cali", 30m)] };

        var informe = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "hoy" }, [], Hoy, Hoy, DateTime.Now, 1, 10);

        informe.Egresos.PremiosPorLoteria.Select(p => (p.Texto, p.Valor, p.Cantidad))
            .Should().Equal(("Bogotá", 120m, 1), ("Cali", 80m, 2));
    }

    [Fact]
    public void Los_numeros_cubiertos_se_listan_y_se_suman_por_loteria()
    {
        var fuente = new FuenteInformeVentas
        {
            Cubierto = new VistaRevisadaManualResponse
            {
                Total = 900m,
                Grupos =
                [
                    Cubierto("Cali", "1234", 200m, new DateTime(2026, 10, 18, 9, 0, 0)),
                    Cubierto("Bogotá", "5678", 500m, new DateTime(2026, 10, 20, 9, 0, 0)),
                    Cubierto("Cali", "9999", 200m, new DateTime(2026, 10, 19, 9, 0, 0))
                ]
            }
        };

        var informe = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "hoy" }, [], Hoy, Hoy, DateTime.Now, 1, 10);
        var porValor = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "hoy" }, [], Hoy, Hoy, DateTime.Now, 1, 10,
            ordenCubiertos: "valor", direccionCubiertos: "desc", paginaCubiertos: 1, filasCubiertos: 2);
        var sinCubierto = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "hoy", TiposEgreso = [VentasInforme.TipoEgresoPremios] }, [], Hoy, Hoy, DateTime.Now, 1, 10);

        informe.Egresos.Cubiertos.Items.Select(c => (c.Fecha, c.Loteria, c.Numero, c.Valor)).Should().Equal(
            (new DateOnly(2026, 10, 20), "Bogotá", "5678", 500m),
            (new DateOnly(2026, 10, 19), "Cali", "9999", 200m),
            (new DateOnly(2026, 10, 18), "Cali", "1234", 200m));
        informe.Egresos.CubiertoPorLoteria.Select(c => (c.Texto, c.Valor, c.Cantidad)).Should().Equal(("Bogotá", 500m, 1), ("Cali", 400m, 2));
        porValor.Egresos.Cubiertos.TotalCount.Should().Be(3);
        porValor.Egresos.Cubiertos.Items.First().Numero.Should().Be("5678");
        sinCubierto.Egresos.Cubiertos.TotalCount.Should().Be(0);
        sinCubierto.Egresos.CubiertoPorLoteria.Should().BeEmpty();
    }

    private static GrupoRevisadaManualResponse Cubierto(string loteria, string numero, decimal valor, DateTime fecha) => new()
    {
        OperacionId = Guid.NewGuid(),
        Loteria = loteria,
        Numero = numero,
        Subtotal = valor,
        FechaOperacion = fecha
    };

    private static NewRich.Application.Contracts.PremiosEntregadosWeb.PremioEntregadoWebResponse Premio(string loteria, decimal valor) =>
        new(Guid.NewGuid(), Hoy, loteria, "1234", 1_000m, valor, DateTime.Now, Guid.NewGuid(), "Ana");

    [Fact]
    public void El_filtro_de_vendedores_ofrece_solo_vendedores_y_muestra_los_elegidos()
    {
        var ana = Guid.NewGuid();
        var beto = Guid.NewGuid();
        var usuarios = new List<NewRich.Application.Contracts.Usuarios.UsuarioResponse>
        {
            new() { UsuarioId = beto, NombreCompleto = "Beto", Usuario = "beto", Rol = NewRich.Domain.Enums.RolUsuario.VendedorWeb },
            new() { UsuarioId = ana, NombreCompleto = "Ana", Usuario = "ana", Rol = NewRich.Domain.Enums.RolUsuario.Vendedor },
            new() { UsuarioId = Guid.NewGuid(), NombreCompleto = "Ciro", Usuario = "ciro", Rol = NewRich.Domain.Enums.RolUsuario.Recaudador }
        };

        var todos = VentasInforme.Construir("ventas", [], new FiltrosPeriodoInforme { Periodo = "7" }, true, Hoy, usuarios);
        var elegidos = VentasInforme.Construir("ventas", [], new FiltrosPeriodoInforme { Periodo = "7", Vendedores = [ana] }, true, Hoy, usuarios);

        todos.Vendedores.Select(v => v.Texto).Should().Equal("Ana", "Beto");
        todos.Detalle.Should().Contain(d => d.Etiqueta == UiTexts.InformeEtiquetaVendedores && d.Valor == UiTexts.InformeTodosVendedores);
        elegidos.Detalle.Should().Contain(d => d.Etiqueta == UiTexts.InformeEtiquetaVendedores && d.Valor == "Ana");
        elegidos.ResumenCorto.Should().EndWith("Ana");
    }

    [Fact]
    public void Las_tarjetas_suman_egresos_y_calculan_la_ganancia()
    {
        var fuente = new FuenteInformeVentas
        {
            Kpi = new KpiResponse { Ingresos = 1_000_000m, VentasConfirmadas = 40, ValorPremiosEntregados = 300_000m, EntregasPremio = 2 },
            Gastos = Gastos(150_000m),
            Cubierto = new VistaRevisadaManualResponse { Total = 700_000m, Jugadas = 9 }
        };

        var informe = VentasInforme.Informe(fuente, new FiltrosPeriodoInforme { Periodo = "hoy" }, [], Hoy, Hoy, DateTime.Now, 1, 10);

        informe.Resumen.TotalIngresos.Should().Be(1_000_000m);
        informe.Resumen.TotalPremios.Should().Be(300_000m);
        informe.Resumen.TotalEgresos.Should().Be(1_150_000m);
        informe.Resumen.Ganancias.Should().Be(-150_000m);
        informe.Resumen.Margen.Should().Be(-15m);
    }

    [Fact]
    public void El_informe_de_egresos_reparte_premio_gastos_y_cubierto()
    {
        var fuente = new FuenteInformeVentas
        {
            Kpi = new KpiResponse { Ingresos = 1_000_000m, ValorPremiosEntregados = 300_000m },
            Gastos = Gastos(150_000m),
            Cubierto = new VistaRevisadaManualResponse { Total = 700_000m, Jugadas = 9 }
        };

        var informe = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "hoy" }, [], Hoy, Hoy, DateTime.Now, 1, 10);

        informe.Resumen.TotalEgresos.Should().Be(1_150_000m);
        informe.Egresos.Conceptos.Select(c => c.Texto).Should().Equal(UiTexts.InformeConceptoCubierto, UiTexts.InformeConceptoPremios, UiTexts.InformeConceptoGastos);
    }

    [Fact]
    public void Los_ingresos_llenan_todos_los_dias_y_listan_cada_venta_sin_agrupar()
    {
        var fuente = new FuenteInformeVentas
        {
            Kpi = new KpiResponse
            {
                Ingresos = 300m,
                VentasConfirmadas = 3,
                VentasPorDiaDetalle = [new() { Fecha = new DateOnly(2026, 10, 19), Ventas = 3, Total = 300m }],
                VentasDetalle =
                [
                    new() { FechaVenta = new DateTime(2026, 10, 19, 9, 0, 0), Vendedor = "Ana", Grupo = "Norte", Total = 50m },
                    new() { FechaVenta = new DateTime(2026, 10, 19, 10, 0, 0), Vendedor = "Bruno", Grupo = "Sur", Total = 100m },
                    new() { FechaVenta = new DateTime(2026, 10, 19, 11, 0, 0), Vendedor = "Ana", Grupo = "Norte", Total = 150m }
                ]
            }
        };

        var informe = VentasInforme.Informe(fuente, new FiltrosPeriodoInforme { Periodo = "rango" }, [], new DateOnly(2026, 10, 18), Hoy, DateTime.Now, 1, 10);

        informe.Ingresos.Dias.Select(d => d.Total).Should().Equal(0m, 300m, 0m);
        informe.Ingresos.MejorDia!.Fecha.Should().Be(new DateOnly(2026, 10, 19));
        informe.Ingresos.PromedioDiario.Should().Be(100m);
        informe.Ingresos.TicketPromedio.Should().Be(100m);
        informe.Ingresos.Vendedores.Items.Select(v => (v.Posicion, v.Vendedor, v.Total, v.FechaVenta.Hour))
            .Should().Equal((1, "Ana", 150m, 11), (2, "Bruno", 100m, 10), (3, "Ana", 50m, 9));
    }

    [Fact]
    public void Los_gastos_por_persona_muestran_cada_gasto_activo_sin_agrupar()
    {
        var usuario = Guid.NewGuid();
        var fuente = new FuenteInformeVentas
        {
            Gastos = Gastos(300m),
            Movimientos =
            [
                Movimiento(usuario, "Carla", "Transporte", 100m, "Activo"),
                Movimiento(usuario, "Carla", "Papelería", 200m, "Activo"),
                Movimiento(usuario, "Carla", "Papelería", 900m, "Anulado")
            ]
        };

        var informe = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "hoy" }, [], Hoy, Hoy, DateTime.Now, 1, 10);

        informe.Egresos.GastosPorPersona.Items.Select(p => (p.Fecha, p.Persona, p.Tipo, p.Total))
            .Should().Equal((Hoy, "Carla", "Papelería", 200m), (Hoy, "Carla", "Transporte", 100m));
    }

    [Fact]
    public void Faltantes_por_recaudar_trae_a_todos_los_que_deben_y_pagina()
    {
        var saldos = Enumerable.Range(1, 7)
            .Select(i => new SaldoVendedorRecaudoResponse { Nombre = $"Vendedor {i}", Grupo = "Centro", Recaudador = "Hugo", TotalPendiente = i * 1_000m })
            .ToList();
        var fuente = new FuenteInformeVentas { Recaudo = new TableroRecaudoResponse { MayoresSaldos = saldos.Take(5).ToList(), SaldosPendientes = saldos } };

        var informe = VentasInforme.InformeRecaudo(fuente, new FiltrosPeriodoInforme { Periodo = "hoy" }, [], Hoy, Hoy, DateTime.Now, paginaAtrasos: 2, filasAtrasos: 5);

        informe.Recaudo!.Atrasos.TotalCount.Should().Be(7);
        informe.Recaudo.Atrasos.Items.Select(a => a.Vendedor).Should().Equal("Vendedor 2", "Vendedor 1");
    }

    [Fact]
    public void Varios_grupos_se_fusionan_en_un_solo_recaudo()
    {
        var recaudador = Guid.NewGuid();
        var fecha = new DateOnly(2026, 10, 20);
        TableroRecaudoResponse Parte(decimal cobrado, decimal pendiente) => new()
        {
            TotalRecaudado = cobrado,
            TotalPendiente = pendiente,
            Dias = [new() { Fecha = fecha, Debia = cobrado + pendiente, Cobrado = cobrado }],
            Recaudadores = [new() { Id = recaudador, Nombre = "Rita", TotalRecaudado = cobrado, TotalPendiente = pendiente, Vendedores = 1 }]
        };

        var fusion = VentasInforme.FusionarTablero([Parte(100m, 50m), Parte(200m, 150m)])!;
        var informe = VentasInforme.InformeRecaudo(new FuenteInformeVentas { Recaudo = fusion }, new FiltrosPeriodoInforme { Periodo = "hoy" }, [], Hoy, Hoy, DateTime.Now);

        informe.Recaudo!.Recaudado.Should().Be(300m);
        informe.Recaudo.Pendiente.Should().Be(200m);
        informe.Recaudo.Avance.Should().Be(60);
        informe.Recaudo.Recaudadores.Should().ContainSingle().Which.Vendedores.Should().Be(2);
        informe.Recaudo.Grafico.Puntos.Should().ContainSingle().Which.Cobrado.Should().Be(300m);
    }

    private static InformeGastosPropiosResponse Gastos(decimal total) =>
        new(Hoy, Hoy, total, 1, total, null, null, null, 0m, 0, [new GastoPorTipoResponse("Transporte", total, 1)], [], 0m, 0m);

    private static MovimientoGastoResponse Movimiento(Guid usuario, string nombre, string tipo, decimal valor, string estado) =>
        new(Guid.NewGuid(), Hoy, Guid.NewGuid(), tipo, null, valor, usuario, nombre, estado, null);
}
