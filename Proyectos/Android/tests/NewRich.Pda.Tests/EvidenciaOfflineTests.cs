using System.Text;
using FluentAssertions;
using NewRich.Application.Contracts.Offline;
using NewRich.Domain.Enums;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Pda.Tests;

public sealed class EvidenciaOfflineTests
{
    [Fact]
    public void Mensaje_de_chat_lleva_consecutivo_y_hora_de_envio()
    {
        var texto = EvidenciaOffline.TextoChat("OFF-000001", new DateTime(2026, 9, 11, 10, 55, 0));

        texto.Should().Be(string.Join(
            Environment.NewLine,
            EvidenciaOffline.MensajeChat,
            "OFF-000001",
            "11/09/2026 10:55"));
    }

    [Fact]
    public void Qr_de_tirilla_reusa_el_cifrado_y_agrega_la_jugada()
    {
        var payload = "payload.cifrado.original";
        var almacenado = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload));
        var draft = TicketDraft.Crear(TipoApuesta.INDIVIDUAL, 10);
        draft.AgregarLinea("1234", 1000, [Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")], ["Cundinamarca"]);

        var json = EvidenciaOffline.QrTirilla(almacenado, "OFF-000001", draft);

        SobreQrOfflineCodec.TryLeer(json, out var sobre).Should().BeTrue();
        sobre.Codigo.Should().Be(payload);
        sobre.Jugada.Lineas.Should().ContainSingle(l => l.Numero == "1234");
        EvidenciaOffline.EsElMismoQr(json, json).Should().BeTrue();
    }

    [Fact]
    public void Adjunto_envia_el_mensaje_oficial_con_el_png_del_qr()
    {
        var enviado = new DateTime(2026, 9, 11, 10, 55, 0);
        var request = EvidenciaOffline.MensajeConQr("OFF-000001", [1, 2, 3], enviado);

        request.Texto.Should().Be(EvidenciaOffline.TextoChat("OFF-000001", enviado));
        request.NombreArchivo.Should().Be("qr-OFF-000001.png");
        request.ContenidoBase64.Should().Be(Convert.ToBase64String([1, 2, 3]));
    }
}
