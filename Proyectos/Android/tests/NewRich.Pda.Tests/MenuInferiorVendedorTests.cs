using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class MenuInferiorVendedorTests
{
    [Fact]
    public void El_menu_tiene_las_cinco_secciones_en_orden()
    {
        MenuInferiorVendedor.Items.Select(i => i.Titulo).Should().Equal(
            PdaTexts.Inicio,
            PdaTexts.Vender,
            PdaTexts.Historico,
            PdaTexts.Soporte,
            PdaTexts.Mas);
        MenuInferiorVendedor.Items.Select(i => i.Ruta).Should().Equal(
            "inicio",
            "vender",
            "historico",
            "soporte",
            "mas");
    }

    [Fact]
    public void La_ruta_activa_sale_de_la_ubicacion_del_shell()
    {
        MenuInferiorVendedor.RutaActiva("//vender").Should().Be("vender");
        MenuInferiorVendedor.RutaActiva("//historico").Should().Be("historico");
        MenuInferiorVendedor.RutaActiva("//inicio").Should().Be("inicio");
        MenuInferiorVendedor.RutaActiva(null).Should().Be("inicio");
    }

    [Fact]
    public void El_menu_queda_en_fila_propia_debajo_del_contenido()
    {
        MenuInferiorVendedor.EsCapa(MenuInferiorVendedor.CapaId).Should().BeTrue();
        MenuInferiorVendedor.EsCapa("otra").Should().BeFalse();
        MenuInferiorVendedor.FilaContenido.Should().Be(0);
        MenuInferiorVendedor.FilaMenu.Should().Be(1);
        MenuInferiorVendedor.FilaMenu.Should().BeGreaterThan(MenuInferiorVendedor.FilaContenido);
    }
}
