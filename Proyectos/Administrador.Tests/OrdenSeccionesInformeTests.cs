using FluentAssertions;
using NewRich.Admin.Services;
using NewRich.Constants;

namespace NewRich.Admin.Tests;

public sealed class OrdenSeccionesInformeTests
{
    [Fact]
    public void Sin_orden_guardado_cada_informe_usa_su_orden_de_siempre()
    {
        var guardado = OrdenSeccionesInforme.Leer(null);

        OrdenSeccionesInforme.Orden(guardado, CategoriasInforme.Ventas)
            .Should().Equal("grupo", "encabezado", "tarjetas", "dias", "vendedores");
        OrdenSeccionesInforme.Orden(OrdenSeccionesInforme.Leer("no es json"), CategoriasInforme.Egresos)
            .Should().Equal("encabezado", "conceptos", "tipos", "loterias", "premios", "cubiertoloterias", "cubiertos", "gastos");
        OrdenSeccionesInforme.Orden(guardado, CategoriasInforme.Vendedores)
            .Should().Equal("encabezado", "tarjetas", "tipos", "grupos", "estado", "tabla");
    }

    [Fact]
    public void Un_orden_guardado_con_los_graficos_juntos_los_deja_separados_en_el_mismo_lugar()
    {
        var guardado = OrdenSeccionesInforme.Leer("""{"vendedores":["tabla","graficos"],"egresos":["graficos","gastos"]}""");

        OrdenSeccionesInforme.Orden(guardado, CategoriasInforme.Vendedores)
            .Should().Equal("tabla", "tipos", "grupos", "estado", "encabezado", "tarjetas");
        OrdenSeccionesInforme.Orden(guardado, CategoriasInforme.Egresos)
            .Should().Equal("conceptos", "tipos", "gastos", "encabezado", "loterias", "premios", "cubiertoloterias", "cubiertos");
    }

    [Fact]
    public void El_orden_guardado_manda_y_las_secciones_que_falten_van_al_final()
    {
        var guardado = OrdenSeccionesInforme.Leer("""{"ventas":["vendedores","desconocida","grupo"]}""");

        OrdenSeccionesInforme.Orden(guardado, CategoriasInforme.Ventas)
            .Should().Equal("vendedores", "grupo", "encabezado", "tarjetas", "dias");
        OrdenSeccionesInforme.Orden(guardado, CategoriasInforme.Recaudo)
            .Should().Equal("encabezado", "tarjetas", "linea", "recaudadores", "atrasos");
    }

    [Fact]
    public void Aplicar_cambia_solo_el_informe_movido_y_conserva_los_demas()
    {
        var actual = """{"egresos":["gastos","encabezado"]}""";

        var json = OrdenSeccionesInforme.Aplicar(actual, CategoriasInforme.Ventas, ["dias", "grupo", "dias", "otra"]);

        json.Should().NotBeNull();
        var leido = OrdenSeccionesInforme.Leer(json);
        OrdenSeccionesInforme.Orden(leido, CategoriasInforme.Ventas).Should().Equal("dias", "grupo", "encabezado", "tarjetas", "vendedores");
        OrdenSeccionesInforme.Orden(leido, CategoriasInforme.Egresos).Should().Equal("gastos", "encabezado", "conceptos", "tipos", "loterias", "premios", "cubiertoloterias", "cubiertos");
    }

    [Fact]
    public void Aplicar_rechaza_un_informe_que_no_existe()
    {
        OrdenSeccionesInforme.Aplicar(null, "utilidades", ["grupo"]).Should().BeNull();
    }
}
