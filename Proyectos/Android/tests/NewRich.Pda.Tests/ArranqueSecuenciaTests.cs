using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class ArranqueSecuenciaTests
{
    [Fact]
    public void Fraccion_avanza_con_cada_paso_real_hasta_uno()
    {
        ArranqueSecuencia.Fraccion(0).Should().Be(0);
        ArranqueSecuencia.Fraccion(ArranqueSecuencia.Pasos.Count).Should().Be(1);
        ArranqueSecuencia.Fraccion(2).Should().BeApproximately(2d / ArranqueSecuencia.Pasos.Count, 0.001);
    }

    [Fact]
    public void Etiqueta_describe_el_paso_que_el_usuario_ve()
    {
        ArranqueSecuencia.Etiqueta(0).Should().Be(PdaTexts.ArranqueAlmacen);
        ArranqueSecuencia.Etiqueta(ArranqueSecuencia.Pasos.Count - 1).Should().Be(PdaTexts.ArranqueListo);
        ArranqueSecuencia.Etiqueta(99).Should().Be(PdaTexts.ArranqueListo);
    }
}
