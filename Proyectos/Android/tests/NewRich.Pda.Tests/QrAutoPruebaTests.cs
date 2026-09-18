using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class QrAutoPruebaTests
{
    [Fact]
    public void Arma_un_cuadro_nv21_del_tamano_que_entrega_la_camara()
    {
        var cuadro = QrAutoPrueba.Nv21(480);

        cuadro.Ancho.Should().Be(480);
        cuadro.Alto.Should().Be(480);
        cuadro.Datos.Length.Should().Be((480 * 480 * 3) / 2);
    }

    [Fact]
    public void El_cuadro_de_prueba_lleva_un_qr_que_si_se_puede_leer()
    {
        // Si el equipo no lee este cuadro, el lector nativo está fallando: el cuadro en sí
        // queda comprobado acá con el lector administrado.
        var cuadro = QrAutoPrueba.Nv21();

        ObservadorLecturaQr.DesdeNv21(cuadro.Datos, cuadro.Ancho, cuadro.Alto)
            .Should().Be(QrAutoPrueba.Contenido);
    }
}
