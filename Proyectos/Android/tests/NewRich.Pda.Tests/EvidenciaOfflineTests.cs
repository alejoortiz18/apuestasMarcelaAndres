using System.Text;
using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class EvidenciaOfflineTests
{
    [Fact]
    public void Mensaje_de_chat_es_el_texto_permanente_del_requerimiento()
    {
        EvidenciaOffline.MensajeChat.Should().Be("CÓDIGO VENDIDO OFFLINE — Favor registrar");
    }

    [Fact]
    public void Qr_del_codigo_local_usa_el_payload_precifrado()
    {
        var payload = "1.key.nonce.cipher.tag";
        var almacenado = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));

        EvidenciaOffline.ContenidoQr(almacenado).Should().Be(payload);
    }

    [Fact]
    public void Adjunto_envia_el_mensaje_oficial_con_el_png_del_qr()
    {
        var request = EvidenciaOffline.MensajeConQr("OFF-000001", [1, 2, 3]);

        request.Texto.Should().Be(EvidenciaOffline.MensajeChat);
        request.NombreArchivo.Should().Be("qr-OFF-000001.png");
        request.ContenidoBase64.Should().Be(Convert.ToBase64String([1, 2, 3]));
    }
}
