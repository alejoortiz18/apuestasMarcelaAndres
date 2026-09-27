using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class AvisoActualizacionTests
{
    [Fact]
    public void El_mensaje_indica_la_version_y_pide_descargar_e_instalar()
    {
        var mensaje = PdaTexts.ActualizacionMensaje("1.1", 78);

        mensaje.Should().Be("Hay una versión nueva (1.1, compilación 78). Descárgala e instálala en este dispositivo.");
    }
}
