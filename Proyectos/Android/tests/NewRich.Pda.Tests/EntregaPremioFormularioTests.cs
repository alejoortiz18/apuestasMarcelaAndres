using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class EntregaPremioFormularioTests
{
    [Fact]
    public void EstaCompleto_exige_datos_y_las_cuatro_fotos()
    {
        EntregaPremioFormulario.EstaCompleto("Juan", "Pérez", "300", "Calle 1", "1000", true, true, true, true)
            .Should().BeTrue();
        EntregaPremioFormulario.EstaCompleto("Juan", "Pérez", "300", "Calle 1", "1000", true, true, false, true)
            .Should().BeFalse();
        EntregaPremioFormulario.EstaCompleto("Juan", "Pérez", "300", "Calle 1", "1000", true, true, true, false)
            .Should().BeFalse();
        EntregaPremioFormulario.EstaCompleto("", "Pérez", "300", "Calle 1", "1000", true, true, true, true)
            .Should().BeFalse();
    }

    [Fact]
    public void La_etiqueta_de_cada_foto_muestra_el_nombre_del_archivo_cargado()
    {
        EntregaPremioFormulario.EtiquetaFoto("IMG_20260915_190000.jpg")
            .Should().Be($"{PdaTexts.FotoCargada} IMG_20260915_190000.jpg");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sin_foto_la_etiqueta_sigue_diciendo_pendiente(string? archivo)
    {
        EntregaPremioFormulario.EtiquetaFoto(archivo).Should().Be(PdaTexts.FotoPendiente);
    }

    [Fact]
    public void El_resumen_encima_del_boton_lista_las_fotos_ya_cargadas()
    {
        var resumen = EntregaPremioFormulario.ResumenFotosCargadas(
            (PdaTexts.FotoTicketConQr, "ticket.jpg"),
            (PdaTexts.FotoGanadorConTicket, null),
            (PdaTexts.FotoCedulaFrente, "frente.jpg"),
            (PdaTexts.FotoCedulaReverso, "   "));

        resumen.Should().Be(string.Join(Environment.NewLine,
            PdaTexts.FotosCargadas,
            $"{PdaTexts.FotoTicketConQr}: ticket.jpg",
            $"{PdaTexts.FotoCedulaFrente}: frente.jpg"));
    }

    [Fact]
    public void Sin_ninguna_foto_cargada_el_resumen_queda_vacio()
    {
        EntregaPremioFormulario.ResumenFotosCargadas(
            (PdaTexts.FotoTicketConQr, null),
            (PdaTexts.FotoGanadorConTicket, null))
            .Should().BeEmpty();
    }
}
