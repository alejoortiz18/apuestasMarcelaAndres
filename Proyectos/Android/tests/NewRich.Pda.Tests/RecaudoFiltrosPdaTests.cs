using FluentAssertions;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Domain.Services;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class RecaudoFiltrosPdaTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 4);

    [Fact]
    public void Hoy_muestra_solo_quien_tiene_ventas_de_hoy_sin_cobrar()
    {
        RecaudoListas.De(Filas(), FiltroCobro.Hoy, "", "vendedor")
            .Select(f => f.NombreCompleto).Should().Equal("Ana", "Dora", "Eva");
    }

    [Fact]
    public void Cobrados_muestra_solo_quien_pago_hoy()
    {
        RecaudoListas.De(Filas(), FiltroCobro.Cobrados, "", "vendedor")
            .Select(f => f.NombreCompleto).Should().Equal("Beto", "Eva");
    }

    [Fact]
    public void Adeudados_muestra_a_quien_arrastra_deuda()
    {
        RecaudoListas.De(Filas(), FiltroCobro.Adeudados, "", "vendedor")
            .Select(f => f.NombreCompleto).Should().Equal("Beto", "Ciro", "Dora");
    }

    [Fact]
    public void Los_filtros_respetan_el_grupo_y_la_busqueda()
    {
        RecaudoListas.De(Filas(), FiltroCobro.Adeudados, "", "vendedor", "Centro")
            .Select(f => f.NombreCompleto).Should().Equal("Beto");
        RecaudoListas.De(Filas(), FiltroCobro.Hoy, "dor", "vendedor")
            .Select(f => f.NombreCompleto).Should().Equal("Dora");
    }

    [Fact]
    public void La_lista_guardada_hoy_se_usa_tal_cual()
    {
        var filas = Filas();

        RecaudoListas.DelDia(filas, "2026-10-04", Hoy).Should().BeSameAs(filas);
    }

    [Fact]
    public void La_lista_guardada_otro_dia_conserva_la_deuda_y_deja_en_cero_lo_de_ese_dia()
    {
        var delDia = RecaudoListas.DelDia(Filas(), "2026-10-03", Hoy);

        delDia.Select(f => f.NombreCompleto).Should().Equal("Ana", "Beto", "Ciro", "Dora", "Eva");
        delDia.Should().OnlyContain(f => f.TotalVendido == 0m && f.ValorACobrar == 0m && f.PagosHoy == 0m && f.PendienteDelDia == 0m && f.UltimoPago == null);
        delDia.Select(f => f.TotalPendiente).Should().Equal(80m, 10m, 25m, 95m, 5m);
        delDia.Select(f => f.SaldoAnterior).Should().Equal(80m, 10m, 25m, 95m, 5m);
        delDia.Should().OnlyContain(f => f.Lista == nameof(ListaCobro.Pendientes) && f.Estado == nameof(EstadoCobro.Deudado));
        RecaudoListas.De(delDia, FiltroCobro.Cobrados, "", "vendedor").Should().BeEmpty();
        RecaudoListas.De(delDia, FiltroCobro.Hoy, "", "vendedor").Should().BeEmpty();
        RecaudoListas.De(delDia, FiltroCobro.Adeudados, "", "vendedor").Should().HaveCount(5);
    }

    [Fact]
    public void Al_cambiar_de_dia_sale_quien_no_debe_nada()
    {
        var alDia = Fila("Fabio", vendido: 100m, generado: 10m, saldoAnterior: 0m, pagado: 10m, pendienteDelDia: 0m);

        RecaudoListas.DelDia([alDia], "2026-10-03", Hoy).Should().BeEmpty();
    }

    [Fact]
    public void Una_lista_guardada_sin_fecha_se_trata_como_de_otro_dia()
    {
        RecaudoListas.DelDia(Filas(), null, Hoy).Should().OnlyContain(f => f.TotalVendido == 0m && f.PagosHoy == 0m);
    }

    [Fact]
    public void Un_cobro_sin_subir_de_otro_dia_baja_la_deuda_sin_contar_como_cobro_de_hoy()
    {
        var ciro = Fila("Ciro", vendido: 0m, generado: 0m, saldoAnterior: 25m, pagado: 0m, pendienteDelDia: 0m);
        var ayer = new PagoPendienteRecaudo(ciro.VendedorId, 10m, "ayer", new DateTime(2026, 10, 3, 20, 0, 0, DateTimeKind.Utc));

        var filas = RecaudoListas.ConPendientes([ciro], [ayer], Hoy);

        var fila = filas.Single();
        fila.TotalPendiente.Should().Be(15m);
        fila.PagosHoy.Should().Be(0m);
        fila.Lista.Should().Be(nameof(ListaCobro.Pendientes));
        RecaudoListas.De(filas, FiltroCobro.Cobrados, "", "vendedor").Should().BeEmpty();
    }

    [Fact]
    public void Un_cobro_sin_subir_de_hoy_lo_pasa_a_cobrados()
    {
        var ana = Fila("Ana", vendido: 800m, generado: 80m, saldoAnterior: 0m, pagado: 0m, pendienteDelDia: 80m);
        var hoy = new PagoPendienteRecaudo(ana.VendedorId, 30m, "hoy", new DateTime(2026, 10, 4, 15, 0, 0, DateTimeKind.Utc));

        var fila = RecaudoListas.ConPendientes([ana], [hoy], Hoy).Single();

        fila.PagosHoy.Should().Be(30m);
        fila.Lista.Should().Be(nameof(ListaCobro.Cobrados));
    }

    [Fact]
    public void Recaudar_abre_en_hoy_y_tiene_hoy_cobrados_y_adeudados()
    {
        var ruta = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "NewRich.Maui", "Views", "Recaudador", "RecaudadorCobroPage.cs"));
        var cobro = File.ReadAllText(ruta);

        cobro.Should().Contain("_filtroLista = FiltroCobro.Hoy");
        cobro.Should().Contain("BotonLista(PdaTexts.Hoy, FiltroCobro.Hoy)");
        cobro.Should().Contain("BotonLista(PdaTexts.Cobrados, FiltroCobro.Cobrados)");
        cobro.Should().Contain("BotonLista(PdaTexts.Adeudados, FiltroCobro.Adeudados)");
        cobro.Should().NotContain("PdaTexts.Pendientes");
    }

    private static List<ObligacionRecaudoResponse> Filas() =>
    [
        Fila("Ana", vendido: 800m, generado: 80m, saldoAnterior: 0m, pagado: 0m, pendienteDelDia: 80m),
        Fila("Beto", vendido: 400m, generado: 40m, saldoAnterior: 0m, pagado: 30m, pendienteDelDia: 0m, grupo: "Centro"),
        Fila("Ciro", vendido: 0m, generado: 0m, saldoAnterior: 25m, pagado: 0m, pendienteDelDia: 0m),
        Fila("Dora", vendido: 500m, generado: 50m, saldoAnterior: 45m, pagado: 0m, pendienteDelDia: 50m),
        Fila("Eva", vendido: 600m, generado: 60m, saldoAnterior: 0m, pagado: 60m, pendienteDelDia: 5m)
    ];

    private static ObligacionRecaudoResponse Fila(
        string nombre,
        decimal vendido,
        decimal generado,
        decimal saldoAnterior,
        decimal pagado,
        decimal pendienteDelDia,
        string grupo = "Sur")
    {
        var pendiente = Math.Max(0m, saldoAnterior + generado - pagado) + (pagado > 0m ? pendienteDelDia : 0m);
        var clasificacion = EstadoCobroRecaudoRegla.Clasificar(saldoAnterior, generado, pagado)!.Value;
        return new ObligacionRecaudoResponse
        {
            VendedorId = Guid.NewGuid(),
            NombreCompleto = nombre,
            Grupo = grupo,
            TotalVendido = vendido,
            ValorACobrar = generado,
            SaldoAnterior = saldoAnterior,
            PagosHoy = pagado,
            PendienteDelDia = pendienteDelDia,
            TotalPendiente = pendiente,
            UltimoPago = pagado > 0m ? new DateTime(2026, 10, 3, 10, 0, 0) : null,
            Estado = clasificacion.Estado.ToString(),
            Color = clasificacion.Color.ToString(),
            Lista = clasificacion.Lista.ToString()
        };
    }
}
