using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class MenuInferiorObservadorTests
{
    [Fact]
    public void El_menu_tiene_las_cinco_secciones_en_el_mismo_orden_que_el_vendedor()
    {
        MenuInferiorObservador.Items.Select(i => i.Titulo).Should().Equal(
            PdaTexts.Inicio,
            PdaTexts.Validar,
            PdaTexts.Consultas,
            PdaTexts.Soporte,
            PdaTexts.Mas);
        MenuInferiorObservador.Items.Select(i => i.Ruta).Should().Equal(
            "oinicio",
            "ovalidar",
            "consultas",
            "osoporte",
            "omas");
    }

    [Fact]
    public void La_ruta_activa_sale_de_la_ubicacion_del_shell()
    {
        MenuInferiorObservador.RutaActiva("//ovalidar").Should().Be("ovalidar");
        MenuInferiorObservador.RutaActiva("//consultas").Should().Be("consultas");
        MenuInferiorObservador.RutaActiva("//oinicio").Should().Be("oinicio");
        MenuInferiorObservador.RutaActiva(null).Should().Be("oinicio");
    }

    [Fact]
    public void El_menu_queda_en_fila_propia_debajo_del_contenido()
    {
        MenuInferiorObservador.EsCapa(MenuInferiorObservador.CapaId).Should().BeTrue();
        MenuInferiorObservador.EsCapa(MenuInferiorVendedor.CapaId).Should().BeFalse();
        MenuInferiorObservador.FilaContenido.Should().Be(0);
        MenuInferiorObservador.FilaMenu.Should().Be(1);
        MenuInferiorObservador.FilaMenu.Should().BeGreaterThan(MenuInferiorObservador.FilaContenido);
    }
}
