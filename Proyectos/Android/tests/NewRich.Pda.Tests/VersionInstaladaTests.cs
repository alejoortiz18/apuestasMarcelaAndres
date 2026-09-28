using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class VersionInstaladaTests
{
    [Fact]
    public void El_texto_muestra_el_nombre_y_la_compilacion_instalada()
    {
        PdaTexts.VersionInstalada("1.0", 79).Should().Be("Compilación 79");
    }
}
