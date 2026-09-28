using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class DescargaActualizacionTests
{
    [Fact]
    public void La_descarga_avanza_hasta_antes_de_abrir_el_instalador()
    {
        var avance = DescargaActualizacion.Descargando(50, 100);

        avance.Fraccion.Should().BeApproximately(0.46, 0.001);
        avance.Etiqueta.Should().Be("Descargando la actualización: 46%");
    }

    [Fact]
    public void Al_abrir_el_instalador_la_barra_queda_completa()
    {
        var avance = DescargaActualizacion.Instalando();

        avance.Fraccion.Should().Be(1);
        avance.Etiqueta.Should().Be(PdaTexts.ActualizacionInstalando);
    }
}
