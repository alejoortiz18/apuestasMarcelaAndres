using FluentAssertions;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Pda.Tests;

public sealed class NombreComboTests
{
    [Fact]
    public void El_juego_de_varios_numeros_se_llama_combo_en_pantalla_y_en_la_tirilla()
    {
        JuegoNumerosManualTexts.Titulo.Should().Be("Combo");
        JuegoNumerosManualTexts.BotonTipo.Should().Be("COMBO");
        JuegoNumerosManualTexts.RequiereConexion.Should().StartWith("Combo ");
        TirillaJuegoNumerosManual.TipoApuesta.Should().Be("COMBO");
    }
}
