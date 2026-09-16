using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class FotoCapturadaTests
{
    [Theory]
    [InlineData("IMG_20260915_184500.jpg", "IMG_20260915_184500.jpg")]
    [InlineData("captura.PNG", "captura.PNG")]
    [InlineData("/storage/emulated/0/DCIM/IMG_01.jpeg", "IMG_01.jpeg")]
    [InlineData("foto.heic", "foto.jpg")]
    [InlineData("captura", "captura.jpg")]
    [InlineData("", "evidencia.jpg")]
    [InlineData(null, "evidencia.jpg")]
    public void El_nombre_conserva_la_foto_original_y_solo_corrige_extensiones_que_el_sistema_no_admite(
        string? original,
        string esperado)
    {
        FotoCapturada.Nombre(original).Should().Be(esperado);
    }
}
