using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class TirillaReglaTests
{
    [Fact]
    public void Cantidad_llena_el_ancho_disponible_sin_pasarse()
    {
        TirillaRegla.Cantidad(100, 5).Should().Be(20);
        TirillaRegla.Cantidad(99, 5).Should().Be(19);
        TirillaRegla.Cantidad(4, 5).Should().Be(1);
        TirillaRegla.De(20).Should().Be(new string('=', 20));
    }
}
