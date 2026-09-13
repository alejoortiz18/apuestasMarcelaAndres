using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class PerfilDispositivoTests
{
    [Theory]
    [InlineData("H10", "SENRAISE")]
    [InlineData("H10P", "alps")]
    [InlineData("PDA-H10", "Senraise")]
    [InlineData("RMX3710", "realme")]
    public void El_vendedor_usa_camara_propia_en_el_pda_y_en_el_celular(string modelo, string fabricante)
    {
        PerfilDispositivo.VendedorUsaCamaraInterna(modelo, fabricante).Should().BeTrue();
        PerfilDispositivo.VendedorUsaEscanerNativo(modelo, fabricante).Should().BeFalse();
    }

    [Theory]
    [InlineData("H10", "SENRAISE")]
    [InlineData("H10P", "alps")]
    [InlineData("PDA-H10", "Senraise")]
    public void El_terminal_sigue_siendo_pda(string modelo, string fabricante)
    {
        PerfilDispositivo.EsPda(modelo, fabricante).Should().BeTrue();
    }

    [Fact]
    public void El_observador_sigue_usando_su_propia_camara()
    {
        PerfilDispositivo.ObservadorUsaCamaraPropia.Should().BeTrue();
    }
}
