using FluentAssertions;
using NewRich.Pda.Core.Actualizacion;

namespace NewRich.Pda.Tests;

public sealed class ApkDescargadoTests
{
    [Fact]
    public void Una_descarga_corta_no_queda_lista_para_instalar()
    {
        ApkDescargado.QuedoCompleto(anunciado: 100, escritos: 40).Should().BeFalse();
        ApkDescargado.QuedoCompleto(anunciado: null, escritos: 0).Should().BeFalse();
    }

    [Fact]
    public void Una_descarga_completa_se_puede_entregar_al_instalador()
    {
        ApkDescargado.QuedoCompleto(anunciado: 100, escritos: 100).Should().BeTrue();
        ApkDescargado.QuedoCompleto(anunciado: null, escritos: 100).Should().BeTrue();
        ApkDescargado.AccionInstalar.Should().Be("android.intent.action.INSTALL_PACKAGE");
        ApkDescargado.AdjuntarUriEnPortapapeles.Should().BeTrue();
    }
}
