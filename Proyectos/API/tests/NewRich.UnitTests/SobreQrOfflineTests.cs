using FluentAssertions;
using NewRich.Application.Contracts.Offline;
using NewRich.Domain.Enums;

namespace NewRich.UnitTests;

public sealed class SobreQrOfflineTests
{
    [Fact]
    public void Arma_json_con_codigo_cifrado_y_jugada_sin_volver_a_cifrar()
    {
        var json = SobreQrOfflineCodec.Armar(
            "payload.cifrado.original",
            "OFF-000001",
            new JugadaOffline
            {
                Tipo = TipoApuesta.INDIVIDUAL.ToString(),
                Fecha = new DateTime(2026, 9, 11, 15, 4, 0, DateTimeKind.Local),
                Total = 2000,
                Lineas =
                [
                    new LineaJugadaOffline
                    {
                        Numero = "1234",
                        Valor = 1000,
                        LoteriaIds = [Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")],
                        Loterias = ["Cundinamarca"]
                    }
                ]
            });

        SobreQrOfflineCodec.TryLeer(json, out var sobre).Should().BeTrue();
        sobre.Codigo.Should().Be("payload.cifrado.original");
        sobre.Consecutivo.Should().Be("OFF-000001");
        sobre.Jugada.Lineas.Should().ContainSingle(l => l.Numero == "1234" && l.Valor == 1000);
    }

    [Fact]
    public void Rechaza_qr_que_no_es_el_sobre()
    {
        SobreQrOfflineCodec.TryLeer("1.key.nonce.cipher", out _).Should().BeFalse();
        SobreQrOfflineCodec.TryLeer("{ }", out _).Should().BeFalse();
    }

    [Fact]
    public void Lee_el_sobre_impreso_aunque_el_lector_deforme_llaves_y_mas()
    {
        const string impreso =
            "¨[codigo[Ñ[1.8f2c1a6e4b094d739e215a7c0b8d3f14.vxW9bD8F6-M8nLaD.pzJxg2-}u002Bh49SdqMFd}u002BLkR6rFBue7ZoJzlKSfmhuYx5f5MAfzkib5evgjAvXbTaj0pPODG42yPlVaGplysUVGZj0RHRa-p1UIUzRAqdI}u002Bv38qOzjNNLtajTUcSDy6iimUph3VzXs2iaQSBhMGKGtKvS-h9btlDFN46E2ZQp5LZNvY5ly9mo0doPltfbviBI-4wDNeoMsX43x3hs6HishLe4OQnasNRvLku83Ci5BQrCzNlrtmYiBE2CtnD}u002B0YQrxC}u002BaO2tPg0Agr}u002BNA¿¿.MCwkRDSjZsns}u002BoxX5nBVxw¿¿[,[consecutivo[Ñ[OFF'000012[,[jugada[Ñ¨[tipo[Ñ[COMBINADO[,[fecha[Ñ[2026'09'11T14Ñ31Ñ30.1651579'05Ñ00[,[total[Ñ20000,[lineas[Ñ´¨[numero[Ñ[890[,[valor[Ñ20000,[loteriaIds[Ñ´[b8103d73'75c5'49d1'a90f'38ea93c2afa7[+,[loterias[Ñ´[Medell}u00EDn[+*+**";

        SobreQrOfflineCodec.TryLeer(impreso, out var sobre).Should().BeTrue();
        sobre.Consecutivo.Should().Be("OFF-000012");
        sobre.Jugada.Tipo.Should().Be("COMBINADO");
        sobre.Jugada.Total.Should().Be(20000);
        sobre.Jugada.Lineas.Should().ContainSingle(l => l.Numero == "890" && l.Valor == 20000);
        sobre.Jugada.Lineas[0].Loterias.Should().Contain("Medellín");
        sobre.Codigo.Should().StartWith("1.8f2c1a6e4b094d739e215a7c0b8d3f14.");
    }

    [Fact]
    public void El_qr_de_tirilla_va_compacto_con_prefijo_para_caber_en_el_papel()
    {
        var json = SobreQrOfflineCodec.Armar(
            "1.key.nonce.cipher",
            "OFF-000002",
            new JugadaOffline
            {
                Tipo = "INDIVIDUAL",
                Fecha = new DateTime(2026, 9, 11, 12, 0, 0, DateTimeKind.Unspecified),
                Total = 1000,
                Lineas =
                [
                    new LineaJugadaOffline
                    {
                        Numero = "890",
                        Valor = 1000,
                        LoteriaIds = [Guid.Parse("b8103d73-75c5-49d1-a90f-38ea93c2afa7")],
                        Loterias = ["Medellín"]
                    }
                ]
            });
        var tirilla = SobreQrOfflineCodec.ParaTirilla(
            "1.key.nonce.cipher",
            "OFF-000002",
            new JugadaOffline
            {
                Tipo = "INDIVIDUAL",
                Fecha = new DateTime(2026, 9, 11, 12, 0, 0, DateTimeKind.Unspecified),
                Total = 1000,
                Lineas =
                [
                    new LineaJugadaOffline
                    {
                        Numero = "890",
                        Valor = 1000,
                        LoteriaIds = [Guid.Parse("b8103d73-75c5-49d1-a90f-38ea93c2afa7")],
                        Loterias = ["Medellín"]
                    }
                ]
            });

        json.Should().StartWith("{");
        tirilla.Should().StartWith("NR3.");
        tirilla.Should().MatchRegex(@"^NR3\.[A-Z2-7]+$");
        tirilla.Length.Should().BeLessThan(json.Length);
        tirilla.Should().NotContain("codigo");
        tirilla.Should().NotContain("{");
        SobreQrOfflineCodec.TryLeer(tirilla, out var sobre).Should().BeTrue();
        sobre.Consecutivo.Should().Be("OFF-000002");
        sobre.EsLlaveCorta.Should().BeFalse();
        sobre.Codigo.Should().Be("1.key.nonce.cipher");
        sobre.Jugada.Tipo.Should().Be("INDIVIDUAL");
        sobre.Jugada.Total.Should().Be(1000);
        sobre.Jugada.Lineas.Should().ContainSingle(l => l.Numero == "890" && l.Valor == 1000);
        sobre.Jugada.Lineas[0].LoteriaIds.Should().Equal(Guid.Parse("b8103d73-75c5-49d1-a90f-38ea93c2afa7"));
    }

    [Fact]
    public void Lee_el_nr3_de_venta_en_linea_aunque_el_formulario_lo_corte_a_400()
    {
        const string escaneado =
            "NR3.AEDTSOJYGQ4TGOAAAAAAAAAAAAAAAAABNYNCZDYJJNZU3HRBLJ6AXDJ7CQU5JTPGB4XFFOLK3DJG2N5BWSPZWYS6FAO2RXXAMSMNPJAAZLFHJPLSW3CS7UNNBHG3ABD2KCCGJ6UCRUIWJ5CZD3ADLH2PIJM6IJTZAF4BPFIYTJBTILBLCKLHH6EFY7UVWNRNNIQBZBD7ZT3IOUJXLIQBV3DUB6M2VLND7CHRQSUDHVPKC7423SYKM7AJVNIJHVIDGS4HXIAXASGURVBHE35EBT3J6BIL7V7G37NF5VQS4YO6ELLJJKXW4KV6SZIIJ5UEISCNY6VIKQCTEQVWNXXZ6H364ZMKYZLWFC6DMAIKZ7MBLVOHGC7CVJPSSSAN7FKBC7GK7WQV22P4";

        escaneado.Length.Should().Be(400);
        SobreQrOfflineCodec.TryLeer(escaneado, out var sobre).Should().BeTrue();
        sobre.Consecutivo.Should().Be("9984938");
        sobre.EsLlaveCorta.Should().BeFalse();
    }

    [Fact]
    public void Lee_el_nr2_aunque_el_lector_cambie_el_guion_del_consecutivo()
    {
        var tirilla = SobreQrOfflineCodec.LlaveCorta("1.key.nonce.cipher.tag", "OFF-000014");
        var leido = tirilla.Replace('-', '\'');

        SobreQrOfflineCodec.TryLeer(leido, out var sobre).Should().BeTrue();
        sobre.Consecutivo.Should().Be("OFF-000014");
        sobre.EsLlaveCorta.Should().BeTrue();
    }

    [Fact]
    public void Rechaza_nr2_con_sello_alterado()
    {
        var tirilla = SobreQrOfflineCodec.LlaveCorta("secreto", "OFF-000003");
        var partes = tirilla.Split('.');
        partes[^1] = "000000";
        var falso = string.Join('.', partes);

        SobreQrOfflineCodec.TryLeer(falso, out var sobre).Should().BeTrue();
        SobreQrOfflineCodec.SelloCoincide("secreto", "OFF-000003", sobre.Sello).Should().BeFalse();
    }

    [Fact]
    public void Lee_el_nr1_pegado_desde_el_lector_del_administrador()
    {
        const string pegado =
            "NR1.RZDJdqJAAEX?pbYJpgYoKM7JQgUEjRNOYJ8sgAIEZAiDLebk3xt70f229763eN8gKHkSl0AFaKREOEAeDUUfMpHLhIUYSZ4cQF?hJELiaP?o9c0JOTByAgN25qjbRUSbvullvnUcexJbZmzO'uryiPeV7in897LZ'NHNHqOXhbzN3HvRZxMtbTFrNs7kenBjL0vHjjirkmJsTG9fFtuKuu2ucuIwLZLnQaLfX6zgNtN3qdHAYaZ7nGAxUSrDTDSe5WmJ68ay7zGU16zYSS6a8HtmYPyxoiYvz82qsiXeLYLeIHuf5cZinmH5fBFZ3ZveMnfFzTqhfY6XG5TUzpGls8iGSMdpNXkQd9a7U7Kcatl0ysztPD0'3JVyvun7KqpN0z6gL7ntTvPyKn6dxu?vI29lH3P5Aa1LtuNbRSnfyKIYAHgdji6aMOja5PZ8e20YAhyCpAGlXexxD6jfoE2qJ7VWmnW0tMP4Y6BRGFwGCDDEVIBMQGiPoYqQStAIiwqisihASYVwcNuy9a5ARc?pV3BNitBrgPrrGxRdHtbPaYmSwbt517L'75VtWCeexZ8uoIgSxrkkyJRyQYQyE?woogLxOVOo4hMahuDzX'tvZ1znYZF44PPn8'fnDw";

        SobreQrOfflineCodec.TryLeer(pegado, out var sobre).Should().BeTrue();
        sobre.Consecutivo.Should().StartWith("OFF-");
        sobre.Jugada.Lineas.Should().NotBeEmpty();
    }
}
