using FluentAssertions;
using NewRich.Admin.Constants;

namespace NewRich.Admin.Tests;

public sealed class CubiertoVentaWebInformeTests
{
    [Fact]
    public void Informes_llama_cubierto_venta_web_al_numero_cubierto()
    {
        UiTexts.InformeConceptoCubierto.Should().Be("Cubierto Venta Web");
        UiTexts.InformeCubiertos.Should().Be("Cubierto Venta Web");
        UiTexts.InformeTipoEgresoCubierto.Should().Be("Cubierto Venta Web");
        UiTexts.CierreCubierto.Should().Be("Cubierto Venta Web");
        UiTexts.InformeTarjetaCubierto.Should().Be("Total Cubierto Venta Web");
        UiTexts.InformeCubiertoPorLoteria.Should().Be("Cubierto Venta Web por lotería");
        UiTexts.InformeEgresosPorConceptoAyuda.Should().Contain("Cubierto Venta Web");
        UiTexts.CierreGananciaFormula.Should().Contain("Cubierto Venta Web");
        UiTexts.InformeCubiertosAyuda.Should().NotContain("número cubierto");
        UiTexts.InformeSinCubiertos.Should().Be("No hay Cubierto Venta Web en el periodo.");
    }
}
