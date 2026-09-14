using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class EntregaPremioFormularioTests
{
    [Fact]
    public void EstaCompleto_exige_datos_y_tres_fotos()
    {
        EntregaPremioFormulario.EstaCompleto("Juan", "Pérez", "300", "Calle 1", "1000", true, true, true)
            .Should().BeTrue();
        EntregaPremioFormulario.EstaCompleto("Juan", "Pérez", "300", "Calle 1", "1000", true, true, false)
            .Should().BeFalse();
        EntregaPremioFormulario.EstaCompleto("", "Pérez", "300", "Calle 1", "1000", true, true, true)
            .Should().BeFalse();
    }

    [Fact]
    public void Pendiente_indica_el_primer_elemento_faltante()
    {
        EntregaPremioFormulario.Pendiente(null, "Pérez", "300", "Calle", "100", true, true, true)
            .Should().Be(PdaTexts.NombreGanador);
        EntregaPremioFormulario.Pendiente("Juan", "Pérez", "300", "Calle", "100", true, false, true)
            .Should().Be(PdaTexts.FotoGanadorConTicket);
        EntregaPremioFormulario.Pendiente("Juan", "Pérez", "300", "Calle", "100", true, true, true)
            .Should().BeNull();
    }
}
