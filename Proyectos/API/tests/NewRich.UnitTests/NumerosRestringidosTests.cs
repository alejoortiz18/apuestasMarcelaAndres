using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class NumerosRestringidosTests
{
    [Fact]
    public void Detecta_un_numero_bloqueado()
    {
        NumerosRestringidos.EstaBloqueado("1234", ["1234", "0001"]).Should().BeTrue();
        NumerosRestringidos.EstaBloqueado(" 123 ", ["123"]).Should().BeTrue();
    }

    [Fact]
    public void Permite_un_numero_que_no_esta_en_la_lista()
    {
        NumerosRestringidos.EstaBloqueado("5678", ["1234"]).Should().BeFalse();
        NumerosRestringidos.EstaBloqueado("1234", []).Should().BeFalse();
        NumerosRestringidos.EstaBloqueado("1234", null).Should().BeFalse();
    }
}
