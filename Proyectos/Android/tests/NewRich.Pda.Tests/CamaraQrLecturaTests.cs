using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class CamaraQrLecturaTests
{
    [Fact]
    public void Avisa_con_un_mensaje_corto_al_detectar_el_codigo()
    {
        PdaTexts.LeyendoCodigo.Should().Be("Leyendo código....");
    }

    [Fact]
    public void La_camara_del_pda_avisa_que_el_qr_se_esta_procesando()
    {
        PdaTexts.CamaraQrProcesando.Should().Be(
            "El QR está siendo procesado. No retire la cámara del QR hasta recibir el mensaje.");
        PdaTexts.LeyendoQr.Should().Be(PdaTexts.CamaraQrProcesando);
    }

    [Fact]
    public void Analiza_cada_cuadro_que_entrega_la_camara()
    {
        CamaraQrLectura.SaltoDeCuadros.Should().Be(1);
        CamaraQrLectura.RotacionSensorGrados.Should().Be(90);
    }

    [Fact]
    public void Pide_una_vista_previa_grande_porque_el_qr_de_la_tirilla_es_denso()
    {
        // A 640x480 el QR impreso a 3 puntos por módulo no alcanza los píxeles por módulo
        // que necesita el decodificador: en el PDA se veía la cámara pero nunca leía.
        CamaraQrLectura.PixelesPreviewObjetivo.Should().BeGreaterThanOrEqualTo(1280 * 720);
        CamaraQrLectura.PixelesPreviewMinimos.Should().BeLessThan(CamaraQrLectura.PixelesPreviewObjetivo);
    }

    [Fact]
    public void Elige_la_vista_previa_mas_cercana_al_objetivo()
    {
        var elegida = CamaraQrLectura.MejorPreview([(640, 480), (1280, 720), (1920, 1080)]);

        elegida.Should().Be((1280, 720));
    }

    [Fact]
    public void Descarta_las_vistas_previas_por_debajo_del_minimo()
    {
        var elegida = CamaraQrLectura.MejorPreview([(176, 144), (320, 240), (1280, 720)]);

        elegida.Should().Be((1280, 720));
    }

    [Fact]
    public void Si_ninguna_alcanza_el_minimo_usa_la_mas_grande_disponible()
    {
        var elegida = CamaraQrLectura.MejorPreview([(176, 144), (320, 240)]);

        elegida.Should().Be((320, 240));
    }

    [Fact]
    public void Sin_vistas_previas_disponibles_no_elige_ninguna()
    {
        CamaraQrLectura.MejorPreview([]).Should().Be((0, 0));
    }

    [Fact]
    public void Espera_a_ml_kit_lo_suficiente_para_que_alcance_a_responder()
    {
        // Con un tope corto cada cuadro se abandonaba antes de que ML Kit contestara y la
        // lectura nunca llegaba: el tope queda solo como red de seguridad.
        CamaraQrLectura.CopiaPorLectura.Should().BeTrue();
        CamaraQrLectura.MsTimeoutMlKit.Should().BeGreaterThanOrEqualTo(3000);
        CamaraQrLectura.UsarMlKitEnVistaPrevia.Should().BeTrue();
    }

    [Fact]
    public void Apaga_ml_kit_cuando_se_le_agota_el_tiempo_varias_veces_seguidas()
    {
        CamaraQrLectura.ApagarMlKit(0).Should().BeFalse();
        CamaraQrLectura.ApagarMlKit(CamaraQrLectura.TiemposAgotadosParaApagarMlKit - 1).Should().BeFalse();
        CamaraQrLectura.ApagarMlKit(CamaraQrLectura.TiemposAgotadosParaApagarMlKit).Should().BeTrue();
    }

    [Fact]
    public void Vuelve_a_enfocar_cada_cierto_tiempo_mientras_busca_el_qr()
    {
        // La escena queda quieta frente a la tirilla: sin reenfoque periódico el cuadro
        // se queda borroso y el decodificador no tiene nada que leer.
        CamaraQrLectura.MsEntreEnfoques.Should().BeInRange(1000, 4000);
    }
}
