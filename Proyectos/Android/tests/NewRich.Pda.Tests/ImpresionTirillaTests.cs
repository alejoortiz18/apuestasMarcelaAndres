using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class ImpresionTirillaTests
{
    [Fact]
    public void El_texto_de_la_tirilla_lleva_avance_para_que_la_impresora_corte()
    {
        var listo = ImpresionTirilla.ParaImpresora("RECIBO\nTOTAL");

        listo.Should().StartWith("RECIBO");
        listo.Should().EndWith("\n\n\n\n");
        listo.Should().Contain("TOTAL");
    }

    [Fact]
    public void El_cuerpo_separa_el_qr_entre_el_total_y_la_leyenda()
    {
        var texto = "TOTAL APOSTADO $1.000\n" + TirillaTexto.MarcaQr + "GRACIAS POR SU COMPRA.";
        var cuerpo = ImpresionTirilla.Cuerpo(texto);

        cuerpo.Antes.Should().Contain("TOTAL APOSTADO");
        cuerpo.Antes.Should().NotContain("GRACIAS");
        cuerpo.Antes.Should().NotContain(TirillaTexto.MarcaQr);
        cuerpo.Despues.Should().StartWith("GRACIAS");
        cuerpo.Despues.Should().NotContain(TirillaTexto.MarcaQr);
    }

    [Fact]
    public void La_letra_es_grande_y_el_modulo_del_qr_en_linea_no_se_reduce()
    {
        ImpresionTirilla.TamanoLetra.Should().Be(28f);
        ImpresionTirilla.ModuloQr.Should().Be(5);
        ImpresionTirilla.ModuloQrPara("1.8f2c1a6e4b094d739e215a7c0b8d3f14.nonce.cipher.tag").Should().Be(5);
    }

    [Fact]
    public void El_qr_offline_imprime_chico_y_al_ancho_del_papel()
    {
        ImpresionTirilla.AnchoQrPuntos.Should().Be(360);
        ImpresionTirilla.ModuloQrOffline.Should().Be(3);
        ImpresionTirilla.ModuloQrPara("NR1.abc_def-ghi").Should().Be(3);
        ImpresionTirilla.ModuloQrPara("""{"codigo":"1.x","consecutivo":"OFF-000001","jugada":{}}""")
            .Should().Be(3);
    }
}
