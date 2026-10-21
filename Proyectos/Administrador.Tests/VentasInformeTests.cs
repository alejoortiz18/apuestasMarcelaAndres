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
    public void Los_ingresos_llenan_todos_los_dias_y_ordenan_el_ranking()
    {
        var fuente = new FuenteInformeVentas
        {
            Kpi = new KpiResponse
            {
                Ingresos = 300m,
                VentasConfirmadas = 3,
                VentasPorDiaDetalle = [new() { Fecha = new DateOnly(2026, 10, 19), Ventas = 3, Total = 300m }],
                IngresosPorVendedor =
                [
                    new() { Vendedor = "Bruno", Grupo = "Sur", Ventas = 1, Total = 100m },
                    new() { Vendedor = "Ana", Grupo = "Norte", Ventas = 2, Total = 200m }
                ]
            }
        };

        var informe = VentasInforme.Informe(fuente, new FiltrosPeriodoInforme { Periodo = "rango" }, [], new DateOnly(2026, 10, 18), Hoy, DateTime.Now, 1, 10);

        informe.Ingresos.Dias.Select(d => d.Total).Should().Equal(0m, 300m, 0m);
        informe.Ingresos.MejorDia!.Fecha.Should().Be(new DateOnly(2026, 10, 19));
        informe.Ingresos.PromedioDiario.Should().Be(100m);
        informe.Ingresos.TicketPromedio.Should().Be(100m);
        informe.Ingresos.Vendedores.Items.Select(v => (v.Posicion, v.Vendedor, v.Participacion)).Should().Equal((1, "Ana", 66.7m), (2, "Bruno", 33.3m));
    }

    [Fact]
    public void Los_gastos_por_persona_solo_cuentan_los_activos()
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

        var persona = informe.Egresos.GastosPorPersona.Items.Should().ContainSingle().Subject;
        persona.Total.Should().Be(300m);
        persona.Cantidad.Should().Be(2);
        persona.TipoPrincipal.Should().Be("Papelería");
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
