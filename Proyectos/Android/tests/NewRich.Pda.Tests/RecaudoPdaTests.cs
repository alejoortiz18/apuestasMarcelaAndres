using FluentAssertions;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Domain.Services;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class RecaudoPdaTests
{
    [Fact]
    public void Pendientes_son_quien_aun_no_pago_hoy_y_cobrados_quien_ya_pago()
    {
        var filas = TresFilas();

        RecaudoListas.De(filas, ListaCobro.Pendientes, "", "vendedor")
            .Select(f => f.NombreCompleto).Should().Equal("Ana");
        RecaudoListas.De(filas, ListaCobro.Cobrados, "", "vendedor")
            .Select(f => f.NombreCompleto).Should().Equal("Beto", "Cata");
        RecaudoListas.De(filas, null, "", "vendedor")
            .Select(f => f.NombreCompleto).Should().Equal("Ana", "Beto", "Cata");
    }

    [Fact]
    public void Los_vendedores_van_agrupados_y_sin_grupo_queda_al_final()
    {
        var grupos = RecaudoListas.Agrupar(TresFilas());

        grupos.Select(g => g.Key).Should().Equal("Centro", "Sin grupo");
        grupos[0].Select(f => f.NombreCompleto).Should().Equal("Ana", "Beto");
        grupos[1].Should().ContainSingle(f => f.SenalSinGrupo);
    }

    [Fact]
    public void El_filtro_busca_por_nombre_o_alias()
    {
        RecaudoListas.De(TresFilas(), null, "bet", "vendedor")
            .Should().ContainSingle(f => f.NombreCompleto == "Beto");
    }

    [Fact]
    public void Un_grupo_muestra_solo_sus_vendedores_y_todos_incluye_sin_grupo()
    {
        var filas = TresFilas();

        RecaudoListas.Grupos(filas).Should().Equal("Centro");
        RecaudoListas.De(filas, null, "", "vendedor", "Centro")
            .Select(f => f.NombreCompleto).Should().Equal("Ana", "Beto");
        RecaudoListas.De(filas, null, "", "vendedor", null)
            .Select(f => f.NombreCompleto).Should().Equal("Ana", "Beto", "Cata");
    }

    [Fact]
    public void La_busqueda_encuentra_usuario_documento_y_una_palabra_incompleta()
    {
        var filas = TresFilas();

        RecaudoListas.De(filas, null, "ale", "vendedor")
            .Should().ContainSingle(f => f.NombreCompleto == "Ana");
        RecaudoListas.De(filas, null, "1098", "vendedor")
            .Should().ContainSingle(f => f.NombreCompleto == "Ana");
    }

    [Fact]
    public void Un_cobro_saca_al_vendedor_de_pendientes_y_lo_deja_en_cobrados()
    {
        var ana = TresFilas()[0];

        var cobrada = RecaudoListas.TrasCobro(ana, 1_000m);

        cobrada.Lista.Should().Be(nameof(ListaCobro.Cobrados));
        cobrada.TotalPendiente.Should().Be(79_000m);
        RecaudoListas.De([cobrada], ListaCobro.Pendientes, "", "vendedor").Should().BeEmpty();
        RecaudoListas.De([cobrada], ListaCobro.Cobrados, "", "vendedor")
            .Should().ContainSingle(f => f.NombreCompleto == "Ana");
    }

    [Fact]
    public void Se_puede_ordenar_por_pendiente_descendente()
    {
        RecaudoListas.De(TresFilas(), null, "", "pendiente")
            .Select(f => f.NombreCompleto).Should().Equal("Ana", "Beto", "Cata");
    }

    [Fact]
    public void El_pago_rechaza_cero_texto_y_un_valor_mayor_que_el_pendiente()
    {
        RecaudoPagoVista.Rechazo(0m, 10_000m).Should().NotBeNull();
        RecaudoPagoVista.Rechazo(-1m, 10_000m).Should().NotBeNull();
        RecaudoPagoVista.Rechazo(10_001m, 10_000m).Should().NotBeNull();
        RecaudoPagoVista.Rechazo(3_000m, 10_000m).Should().BeNull();
        RecaudoPagoVista.TryParsePesos("abc", out _).Should().BeFalse();
        RecaudoPagoVista.TryParsePesos("3.000", out var valor).Should().BeTrue();
        valor.Should().Be(3000m);
    }

    [Fact]
    public void El_modal_muestra_pendiente_recibido_y_saldo_que_queda()
    {
        var modal = RecaudoPagoVista.Confirmar("Ana", 130_000m, 30_000m);

        modal.Vendedor.Should().Be("Ana");
        modal.Pendiente.Should().Be(130_000m);
        modal.Recibido.Should().Be(30_000m);
        modal.SaldoQueQueda.Should().Be(100_000m);
    }

    [Fact]
    public void La_tirilla_lleva_recaudador_vendedor_fecha_valor_y_saldo_sin_consecutivo()
    {
        var texto = TirillaCobroTexto.De(
            "Carmen Recaudo",
            "Ana Pérez",
            new DateTime(2026, 9, 29, 10, 15, 0),
            30_000m,
            100_000m);

        texto.Should().Contain("COMPROBANTE DE COBRO");
        texto.Should().Contain("Carmen Recaudo");
        texto.Should().Contain("Ana Pérez");
        texto.Should().Contain("2026-09-29");
        texto.Should().Contain("10:15");
        texto.Should().NotContain("Consecutivo");
        var lineas = texto.Split(Environment.NewLine);
        lineas.Should().OnlyContain(l => l.Length <= TirillaTexto.AnchoImpresora);
        lineas.Should().Contain(l => l.StartsWith("VALOR RECIBIDO") && l.EndsWith("$30.000") && l.Length == TirillaTexto.AnchoImpresora);
        lineas.Should().Contain(l => l.StartsWith("SALDO QUE QUEDA") && l.EndsWith("$100.000") && l.Length == TirillaTexto.AnchoImpresora);
    }

    [Fact]
    public void La_tirilla_parte_los_nombres_largos_sin_pasarse_del_ancho()
    {
        var texto = TirillaCobroTexto.De(
            "Carmen Recaudo",
            "María Fernanda Gutiérrez de la Torre",
            new DateTime(2026, 9, 29, 10, 15, 0),
            1_500_000m,
            12_000_000m);

        var lineas = texto.Split(Environment.NewLine);
        lineas.Should().OnlyContain(l => l.Length <= TirillaTexto.AnchoImpresora);
        texto.Should().Contain("María Fernanda");
        texto.Should().Contain("Torre");
        lineas.Should().Contain(l => l.EndsWith("$12.000.000"));
    }

    [Fact]
    public void Un_reintento_con_la_misma_clave_no_duplica_el_pago_offline()
    {
        var primero = new PagoPendienteRecaudo(Guid.NewGuid(), 1000m, "clave-1", DateTime.UtcNow);
        var cola = RecaudoColaPagos.Encolar(primero, [primero]);

        cola.Should().ContainSingle(p => p.ClaveIdempotencia == "clave-1");
    }

    [Fact]
    public void El_menu_del_recaudador_tiene_cinco_secciones_propias()
    {
        MenuInferiorRecaudador.Items.Select(i => i.Ruta).Should().Equal(
            "recaudo", "rhistorial", "rmetricas", "rsoporte", "rmas");
        MenuInferiorRecaudador.RutaActiva("//rmetricas").Should().Be("rmetricas");
        MenuInferiorRecaudador.RutaActiva(null).Should().Be("recaudo");
        MenuInferiorRecaudador.EsCapa(MenuInferiorVendedor.CapaId).Should().BeFalse();
    }

    [Fact]
    public void El_resumen_suma_lo_por_recaudar_de_todos_y_cuenta_grupos_y_vendedores()
    {
        var filas = TresFilas();
        filas.Add(new()
        {
            NombreCompleto = "Dina",
            Grupo = "Norte",
            ValorACobrar = 5_000m
        });

        var resumen = RecaudoListas.Resumen(filas);

        resumen.TotalPorRecaudar.Should().Be(135_000m);
        resumen.TotalRecaudado.Should().Be(40_000m);
        resumen.Grupos.Should().Be(2);
        resumen.Vendedores.Should().Be(4);
    }

    [Fact]
    public void La_fecha_del_dia_es_la_de_colombia_cuando_se_conecta()
    {
        var utc = new DateTime(2026, 10, 2, 3, 30, 0, DateTimeKind.Utc);

        RecaudoListas.FechaDelDia(utc).Should().Be("1 de octubre de 2026");
    }

    [Fact]
    public void El_inicio_muestra_recaudar_y_el_cobro_esta_en_su_pantalla()
    {
        var inicio = File.ReadAllText(RutaMaui("Views", "Recaudador", "RecaudadorHomePage.cs"));
        var cobro = File.ReadAllText(RutaMaui("Views", "Recaudador", "RecaudadorCobroPage.cs"));

        inicio.Should().Contain("PdaTexts.Recaudar");
        inicio.Should().Contain("PdaTexts.TotalRecaudar");
        inicio.Should().Contain("PdaTexts.TotalRecaudado");
        inicio.Should().Contain("Ui.Gold");
        var fecha = inicio.IndexOf("RecaudoListas.FechaDelDia", StringComparison.Ordinal);
        var recaudar = inicio.IndexOf("PdaTexts.TotalRecaudar", StringComparison.Ordinal);
        var recaudado = inicio.IndexOf("PdaTexts.TotalRecaudado", StringComparison.Ordinal);
        var resumen = inicio.IndexOf("PdaTexts.ResumenTurno", StringComparison.Ordinal);
        var accesos = inicio.IndexOf("PdaTexts.AccesosRapidos", StringComparison.Ordinal);
        fecha.Should().BeGreaterThan(-1);
        recaudar.Should().BeGreaterThan(fecha);
        recaudado.Should().BeGreaterThan(recaudar);
        resumen.Should().BeGreaterThan(recaudado);
        accesos.Should().BeGreaterThan(resumen);
        inicio.Should().Contain("PdaTexts.GruposAsignados");
        inicio.Should().Contain("PdaTexts.VendedoresAsignados");
        inicio.Should().Contain("RecaudoListas.Resumen");
        inicio.Should().NotContain("RegistrarPagoRecaudo");
        cobro.Should().Contain("PdaTexts.Cobrados");
        cobro.Should().Contain("PdaTexts.Todos");
        cobro.Should().Contain("PdaTexts.Buscar");
        cobro.Should().Contain("RecaudoPagoVista.CifrasTarjeta");
        cobro.Should().Contain("PdaTexts.TotalPagado");
        cobro.Should().Contain("RegistrarPagoRecaudo");
    }

    [Fact]
    public void La_tarjeta_cobrada_muestra_lo_que_el_vendedor_pago()
    {
        var cifras = RecaudoPagoVista.CifrasTarjeta(TresFilas()[1]);

        cifras.Select(c => c.Etiqueta).Should().Equal(
            PdaTexts.VendidoHoy, PdaTexts.SaldoAnterior, PdaTexts.TotalPagado, PdaTexts.PendienteDelDia);
        cifras.Should().Contain(c => c.Etiqueta == PdaTexts.TotalPagado && c.Valor == 30_000m);
        cifras.Should().Contain(c => c.Etiqueta == PdaTexts.PendienteDelDia && c.Valor == 0m);
        cifras.Should().Contain(c => c.Etiqueta == PdaTexts.SaldoAnterior && c.Valor == 10_000m);
    }

    [Fact]
    public void La_tarjeta_pendiente_no_muestra_pagado()
    {
        RecaudoPagoVista.CifrasTarjeta(TresFilas()[0])
            .Select(c => c.Etiqueta).Should().Equal(PdaTexts.VendidoHoy, PdaTexts.SaldoAnterior, PdaTexts.PendienteDelDia);
    }

    [Fact]
    public void Sin_cobro_lo_de_hoy_es_pendiente_del_dia_y_no_pasa_al_saldo_anterior()
    {
        var fila = Fila(saldoAnterior: 0m, aCobrarHoy: 15_000m, pagadoHoy: 0m, pendienteDelDia: 15_000m);

        var saldos = RecaudoPagoVista.Saldos(fila);

        saldos.PendienteDelDia.Should().Be(15_000m);
        saldos.SaldoAnterior.Should().Be(0m);
        saldos.TotalAPagar.Should().Be(15_000m);
        RecaudoPagoVista.CifrasTarjeta(fila).Should().Contain(c => c.Etiqueta == PdaTexts.SaldoAnterior && c.Valor == 0m);
    }

    [Fact]
    public void Tras_el_cobro_lo_que_queda_pasa_al_saldo_anterior_y_se_suma_a_la_deuda_vieja()
    {
        var saldos = RecaudoPagoVista.Saldos(Fila(saldoAnterior: 2_000m, aCobrarHoy: 15_000m, pagadoHoy: 10_000m, pendienteDelDia: 0m));

        saldos.PendienteDelDia.Should().Be(0m);
        saldos.SaldoAnterior.Should().Be(7_000m);
        saldos.TotalAPagar.Should().Be(7_000m);
    }

    [Fact]
    public void Lo_vendido_despues_del_cobro_es_pendiente_del_dia()
    {
        var saldos = RecaudoPagoVista.Saldos(Fila(saldoAnterior: 0m, aCobrarHoy: 18_000m, pagadoHoy: 10_000m, pendienteDelDia: 3_000m));

        saldos.PendienteDelDia.Should().Be(3_000m);
        saldos.SaldoAnterior.Should().Be(5_000m);
        saldos.TotalAPagar.Should().Be(8_000m);
    }

    [Fact]
    public void Al_cobrar_sin_conexion_lo_que_queda_pasa_al_saldo_anterior_y_sigue_cobrado()
    {
        var fila = RecaudoListas.TrasCobro(Fila(saldoAnterior: 0m, aCobrarHoy: 15_000m, pagadoHoy: 0m, pendienteDelDia: 15_000m), 10_000m);

        var saldos = RecaudoPagoVista.Saldos(fila);

        saldos.PendienteDelDia.Should().Be(0m);
        saldos.SaldoAnterior.Should().Be(5_000m);
        fila.Lista.Should().Be(nameof(ListaCobro.Cobrados));
    }

    [Fact]
    public void La_tarjeta_muestra_total_a_pagar_y_no_deja_recibir_mas_de_eso()
    {
        var cobro = File.ReadAllText(RutaMaui("Views", "Recaudador", "RecaudadorCobroPage.cs"));

        cobro.Should().Contain("PdaTexts.TotalAPagar");
        cobro.Should().Contain("RecaudoPagoVista.Rechazo(valor, saldos.TotalAPagar)");
        RecaudoPagoVista.Rechazo(13_051m, 13_050m).Should().Be(PdaTexts.PagoRecaudoExcede);
        PdaTexts.PagoRecaudoExcede.Should().Contain("total a pagar");
    }

    private static ObligacionRecaudoResponse Fila(decimal saldoAnterior, decimal aCobrarHoy, decimal pagadoHoy, decimal pendienteDelDia) => new()
    {
        PendienteDelDia = pendienteDelDia,
        VendedorId = Guid.NewGuid(),
        NombreCompleto = "Vendedor",
        Grupo = "Centro",
        TotalVendido = 67_000m,
        ValorACobrar = aCobrarHoy,
        SaldoAnterior = saldoAnterior,
        PagosHoy = pagadoHoy,
        TotalPendiente = saldoAnterior + aCobrarHoy - pagadoHoy,
        Lista = pagadoHoy > 0m ? nameof(ListaCobro.Cobrados) : nameof(ListaCobro.Pendientes)
    };

    private static string RutaMaui(params string[] partes)
    {
        var ruta = Path.GetFullPath(Path.Combine(
            [AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "NewRich.Maui", .. partes]));
        File.Exists(ruta).Should().BeTrue($"se esperaba el archivo en {ruta}");
        return ruta;
    }

    private static List<ObligacionRecaudoResponse> TresFilas() =>
    [
        new()
        {
            VendedorId = Guid.NewGuid(),
            NombreCompleto = "Ana",
            Alias = "ani",
            Usuario = "alejo",
            Documento = "1098000111",
            Grupo = "Centro",
            TotalVendido = 100_000m,
            ValorACobrar = 80_000m,
            TotalPendiente = 80_000m,
            PagosHoy = 0m,
            Lista = nameof(ListaCobro.Pendientes),
            Estado = nameof(EstadoCobro.PorCobrar),
            Color = nameof(ColorCobro.Verde)
        },
        new()
        {
            VendedorId = Guid.NewGuid(),
            NombreCompleto = "Beto",
            Alias = "beto",
            Grupo = "Centro",
            TotalVendido = 50_000m,
            ValorACobrar = 40_000m,
            TotalPendiente = 10_000m,
            PagosHoy = 30_000m,
            Lista = nameof(ListaCobro.Cobrados),
            Estado = nameof(EstadoCobro.Deudado),
            Color = nameof(ColorCobro.Rojo)
        },
        new()
        {
            VendedorId = Guid.NewGuid(),
            NombreCompleto = "Cata",
            Alias = "cata",
            Grupo = "Sin grupo",
            TotalVendido = 20_000m,
            ValorACobrar = 10_000m,
            TotalPendiente = 0m,
            PagosHoy = 10_000m,
            Lista = nameof(ListaCobro.Cobrados),
            Estado = nameof(EstadoCobro.AlDia),
            Color = nameof(ColorCobro.Azul),
            SenalSinGrupo = true
        }
    ];
}
