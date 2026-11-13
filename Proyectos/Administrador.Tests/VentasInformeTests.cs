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
    public void Los_premios_por_loteria_suman_los_del_observador_y_los_del_vendedor_web()
    {
        var fuente = new FuenteInformeVentas
        {
            Kpi = new KpiResponse
            {
                ValorPremiosEntregados = 200m,
                EntregasPremio = 1,
                PremiosEntregadosPorLoteria =
                [
                    new() { Loteria = "Cali", Valor = 200m, Cantidad = 1 }
                ]
            },
            PremiosWeb = [Premio("Cali", 50m), Premio("Bogotá", 120m)]
        };

        var informe = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "hoy" }, [], Hoy, Hoy, DateTime.Now, 1, 10);

        informe.Egresos.PremiosPorLoteria.Select(p => (p.Texto, p.Valor, p.Cantidad))
            .Should().Equal(("Cali", 250m, 2), ("Bogotá", 120m, 1));
        informe.Egresos.PremiosPorLoteria.Sum(p => p.Valor).Should().Be(informe.Resumen.TotalPremios);
    }

    [Fact]
    public void FusionarKpi_suma_los_premios_del_observador_por_loteria()
    {
        var fusion = VentasInforme.FusionarKpi(
        [
            new KpiResponse
            {
                ValorPremiosEntregados = 200m,
                EntregasPremio = 1,
                PremiosEntregadosPorLoteria = [new() { Loteria = "Cali", Valor = 200m, Cantidad = 1 }]
            },
            new KpiResponse
            {
                ValorPremiosEntregados = 80m,
                EntregasPremio = 1,
                PremiosEntregadosPorLoteria = [new() { Loteria = "Cali", Valor = 50m, Cantidad = 1 }, new() { Loteria = "Bogotá", Valor = 30m, Cantidad = 1 }]
            }
        ])!;

        fusion.ValorPremiosEntregados.Should().Be(280m);
        fusion.PremiosEntregadosPorLoteria.Select(p => (p.Loteria, p.Valor, p.Cantidad))
            .Should().Equal(("Cali", 250m, 2), ("Bogotá", 30m, 1));
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
            ordenCubiertos: "subtotal", direccionCubiertos: "desc", paginaCubiertos: 1, filasCubiertos: 2);
        var sinCubierto = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "hoy", TiposEgreso = [VentasInforme.TipoEgresoPremios] }, [], Hoy, Hoy, DateTime.Now, 1, 10);

        informe.Egresos.Cubiertos.Items.Select(c => (c.Fecha, c.Loteria, c.Numero, c.CubiertoPor, c.Subtotal, c.Descuento, c.Pagado)).Should().Equal(
            (new DateOnly(2026, 10, 20), "Bogotá", "5678", "Dora", 500m, 60m, 540m),
            (new DateOnly(2026, 10, 19), "Cali", "9999", "Dora", 200m, 24m, 216m),
            (new DateOnly(2026, 10, 18), "Cali", "1234", "Dora", 200m, 24m, 216m));
        informe.Egresos.CubiertoPorLoteria.Select(c => (c.Texto, c.Valor, c.Cantidad)).Should().Equal(("Bogotá", 500m, 1), ("Cali", 400m, 2));
        porValor.Egresos.Cubiertos.TotalCount.Should().Be(3);
        porValor.Egresos.Cubiertos.Items.First().Numero.Should().Be("5678");
        sinCubierto.Egresos.Cubiertos.TotalCount.Should().Be(0);
        sinCubierto.Egresos.CubiertoPorLoteria.Should().BeEmpty();
    }

    [Fact]
    public void El_cubierto_del_pda_entra_en_egresos_por_concepto_y_en_su_tabla()
    {
        var fuente = new FuenteInformeVentas
        {
            Cubierto = new VistaRevisadaManualResponse { Total = 700m, Jugadas = 1 },
            CubiertoPda = new VistaRevisadaManualResponse
            {
                Total = 400m,
                Jugadas = 1,
                Grupos = [Cubierto("Medellín", "2468", 400m, new DateTime(2026, 10, 20, 11, 0, 0))]
            }
        };

        var informe = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "hoy" }, [], Hoy, Hoy, DateTime.Now, 1, 10);
        var oculto = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "hoy", TiposEgreso = [VentasInforme.TipoEgresoCubierto] }, [], Hoy, Hoy, DateTime.Now, 1, 10);

        informe.Egresos.Conceptos.Should().Contain(c => c.Texto == UiTexts.InformeConceptoCubiertoPda && c.Valor == 400m);
        informe.Egresos.CubiertosPda.Items.Should().ContainSingle(c => c.Loteria == "Medellín" && c.Numero == "2468" && c.Subtotal == 400m && c.Pagado == 432m);
        informe.Resumen.TotalEgresos.Should().Be(1_100m);
        informe.Egresos.IncluyeCubiertoPda.Should().BeTrue();
        oculto.Egresos.CubiertosPda.TotalCount.Should().Be(0);
        oculto.Egresos.Conceptos.Should().NotContain(c => c.Texto == UiTexts.InformeConceptoCubiertoPda);
        oculto.Resumen.TotalEgresos.Should().Be(700m);
    }

    [Fact]
    public void El_cubierto_pda_del_informe_usa_el_subtotal_con_comision_del_historico()
    {
        var fuente = new FuenteInformeVentas
        {
            CubiertoPda = new VistaRevisadaManualResponse
            {
                Total = 1_000m,
                Jugadas = 1,
                Grupos =
                [
                    new GrupoRevisadaManualResponse
                    {
                        Loteria = "Bogotá",
                        Numero = "321",
                        Subtotal = 1_000m,
                        TotalConComision = 1_050m,
                        FechaOperacion = new DateTime(2026, 10, 20, 22, 3, 0),
                        CubiertoPor = "Dora",
                        DescuentoPorcentaje = 10m,
                        DescuentoValor = 105m,
                        TotalPagar = 945m
                    }
                ]
            }
        };

        var informe = VentasInforme.InformeEgresos(fuente, new FiltrosPeriodoInforme { Periodo = "hoy" }, [], Hoy, Hoy, DateTime.Now, 1, 10);
        var fila = informe.Egresos.CubiertosPda.Items.Should().ContainSingle().Subject;

        fila.SubtotalConComision.Should().Be(1_050m);
        informe.Egresos.Conceptos.Should().Contain(c => c.Texto == UiTexts.InformeConceptoCubiertoPda && c.Valor == 1_050m);
        informe.Resumen.TotalEgresos.Should().Be(1_050m);
        File.ReadAllText(RutaVistaEgresos()).Should().Contain("InformeColumnaSubtotalConComision");
    }

    private static string RutaVistaEgresos() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "Administrador", "Views", "Informes", "_InformeEgresos.cshtml"));

    private static GrupoRevisadaManualResponse Cubierto(string loteria, string numero, decimal valor, DateTime fecha) => new()
    {
        OperacionId = Guid.NewGuid(),
        Loteria = loteria,
        Numero = numero,
        Subtotal = valor,
        FechaOperacion = fecha,
        CubiertoPor = "Dora",
        DescuentoPorcentaje = 10m,
        DescuentoValor = valor * 1.2m * 0.1m,
        TotalPagar = valor * 1.2m * 0.9m
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
    public void Las_opciones_de_vendedor_llevan_el_grupo_para_filtrar_el_listado()
    {
        var norte = Guid.NewGuid();
        var ana = Guid.NewGuid();
        var maria = Guid.NewGuid();
        var opciones = VentasInforme.OpcionesVendedor(
        [
            new() { UsuarioId = ana, NombreCompleto = "Ana", Usuario = "ana", Rol = NewRich.Domain.Enums.RolUsuario.Vendedor, GrupoId = norte, GrupoNombre = "norte" },
            new() { UsuarioId = maria, NombreCompleto = "Maria Jose", Usuario = "mariajose", Rol = NewRich.Domain.Enums.RolUsuario.VendedorWeb }
        ]);

        opciones.Should().ContainSingle(v => v.Texto == "Ana" && v.Grupo == norte.ToString());
        opciones.Should().ContainSingle(v => v.Texto == "Maria Jose" && v.Grupo == string.Empty);
        VentasInforme.VendedoresDeLosGrupos(opciones, [norte]).Select(v => v.Texto).Should().Equal("Ana");
        VentasInforme.VendedoresDeLosGrupos(opciones, null).Select(v => v.Texto).Should().Equal("Ana", "Maria Jose");
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
        informe.Egresos.Conceptos.Select(c => c.Texto).Should().Equal(UiTexts.InformeConceptoCubierto, UiTexts.InformeConceptoPremios, UiTexts.InformeConceptoGastos, UiTexts.InformeConceptoCubiertoPda);
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

    [Fact]
    public void Informe_general_solo_muestra_totales_de_toda_la_empresa()
    {
        var fecha = new DateOnly(2026, 11, 10);
        var fuente = new FuenteInformeVentas
        {
            Recaudo = new TableroRecaudoResponse
            {
                TotalEntregarAdministracion = 120_000m,
                TotalEntregadoAdministracion = 80_000m,
                PeriodoRecortado = false
            }
        };

        var informe = VentasInforme.InformeGeneral(
            fuente,
            new FiltrosPeriodoInforme { Periodo = "hoy" },
            fecha,
            fecha,
            DateTime.Now);

        informe.TotalPorRecibir.Should().Be(120_000m);
        informe.Recibido.Should().Be(80_000m);
        informe.EnDeuda.Should().BeTrue();
        informe.Alcance.Should().Be(UiTexts.InformeTodosGrupos);
    }

    [Fact]
    public void Informe_recaudo_muestra_entrega_a_administracion_y_recaudadores_deudores()
    {
        var fecha = new DateOnly(2026, 11, 10);
        var fuente = new FuenteInformeVentas
        {
            Recaudo = new TableroRecaudoResponse
            {
                TotalEntregarAdministracion = 85_500m,
                TotalEntregadoAdministracion = 30_000m,
                Dias =
                [
                    new()
                    {
                        Fecha = fecha,
                        Debia = 100_000m,
                        Cobrado = 40_000m,
                        Generado = 90_000m,
                        EntregarAdministracion = 85_500m,
                        EntregadoAdministracion = 30_000m
                    }
                ],
                RecaudadoresDeuda =
                [
                    new()
                    {
                        Nombre = "Luis",
                        Grupos = "sur",
                        Recibir = 85_500m,
                        Entregado = 30_000m,
                        Debe = 55_500m
                    },
                    new()
                    {
                        Nombre = "Jose",
                        Grupos = "norte",
                        Recibir = 10_000m,
                        Entregado = 10_000m,
                        Debe = 0m
                    }
                ]
            }
        };

        var informe = VentasInforme.InformeRecaudo(fuente, new FiltrosPeriodoInforme { Periodo = "hoy" }, [], fecha, fecha, DateTime.Now);

        informe.Recaudo!.DebeEntregar.Should().Be(85_500m);
        informe.Recaudo.ValorEntregado.Should().Be(30_000m);
        informe.Recaudo.EnDeudaAdministracion.Should().BeTrue();
        informe.Recaudo.Grafico.Puntos.Should().ContainSingle().Which.EntregarAdministracion.Should().Be(85_500m);
        informe.Recaudo.Grafico.Puntos[0].EntregadoAdministracion.Should().Be(30_000m);
        informe.Recaudo.RecaudadoresDeuda.Items.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new RecaudadorDeudaInforme
            {
                Nombre = "Luis",
                Grupos = "sur",
                Recibir = 85_500m,
                Entregado = 30_000m,
                Debe = 55_500m
            });
    }

    [Fact]
    public void Fusionar_tablero_suma_entrega_a_administracion_y_deudores()
    {
        var fecha = new DateOnly(2026, 11, 10);
        var fusion = VentasInforme.FusionarTablero(
        [
            new TableroRecaudoResponse
            {
                TotalEntregarAdministracion = 50_000m,
                TotalEntregadoAdministracion = 20_000m,
                Dias = [new() { Fecha = fecha, EntregarAdministracion = 50_000m, EntregadoAdministracion = 20_000m }],
                RecaudadoresDeuda = [new() { Nombre = "Luis", Grupos = "sur", Recibir = 50_000m, Entregado = 20_000m, Debe = 30_000m }]
            },
            new TableroRecaudoResponse
            {
                TotalEntregarAdministracion = 35_500m,
                TotalEntregadoAdministracion = 10_000m,
                Dias = [new() { Fecha = fecha, EntregarAdministracion = 35_500m, EntregadoAdministracion = 10_000m }],
                RecaudadoresDeuda = [new() { Nombre = "Luis", Grupos = "norte", Recibir = 35_500m, Entregado = 10_000m, Debe = 25_500m }]
            }
        ])!;

        fusion.TotalEntregarAdministracion.Should().Be(85_500m);
        fusion.TotalEntregadoAdministracion.Should().Be(30_000m);
        fusion.Dias.Should().ContainSingle();
        fusion.Dias[0].EntregarAdministracion.Should().Be(85_500m);
        fusion.Dias[0].EntregadoAdministracion.Should().Be(30_000m);
        var luis = fusion.RecaudadoresDeuda.Should().ContainSingle().Subject;
        luis.Nombre.Should().Be("Luis");
        luis.Recibir.Should().Be(85_500m);
        luis.Entregado.Should().Be(30_000m);
        luis.Debe.Should().Be(55_500m);
        luis.Grupos.Should().Contain("sur").And.Contain("norte");
    }

    private static InformeGastosPropiosResponse Gastos(decimal total) =>
        new(Hoy, Hoy, total, 1, total, null, null, null, 0m, 0, [new GastoPorTipoResponse("Transporte", total, 1)], [], 0m, 0m);

    private static MovimientoGastoResponse Movimiento(Guid usuario, string nombre, string tipo, decimal valor, string estado) =>
        new(Guid.NewGuid(), Hoy, Guid.NewGuid(), tipo, null, valor, usuario, nombre, estado, null);
}
