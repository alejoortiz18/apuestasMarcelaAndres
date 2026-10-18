using FluentAssertions;
using NewRich.Application.Contracts.JuegoNumerosManual;
using NewRich.Constants.Messages;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Ventas;
using NewRich.Shared.Results;

namespace NewRich.Pda.Tests;

public sealed class JuegoNumerosManualTests
{
    private static readonly Guid Chontico = Guid.NewGuid();
    private static readonly Guid Medellin = Guid.NewGuid();

    private static JuegoNumerosManualBorrador Borrador(decimal valor, params string[] numeros)
    {
        var borrador = new JuegoNumerosManualBorrador();
        borrador.Seleccionar(Chontico, "Chontico", true);
        borrador.Seleccionar(Medellin, "Medellín", true);
        borrador.CambiarValor(valor);
        for (var i = 0; i < numeros.Length; i++)
        {
            if (i > 0)
            {
                borrador.AgregarLinea();
            }

            borrador.CambiarNumero(i, numeros[i]);
        }

        return borrador;
    }

    [Fact]
    public void El_mas_queda_solo_en_la_ultima_linea_al_agregar_y_quitar()
    {
        var borrador = Borrador(10_000, "1234", "4323", "5676");

        borrador.EsUltimaLinea(2).Should().BeTrue();
        borrador.EsUltimaLinea(1).Should().BeFalse();

        borrador.QuitarLinea(2);

        borrador.Lineas.Should().Equal("1234", "4323");
        borrador.EsUltimaLinea(1).Should().BeTrue();
    }

    [Fact]
    public void Quitar_la_unica_linea_la_deja_vacia()
    {
        var borrador = Borrador(10_000, "1234");

        borrador.QuitarLinea(0);

        borrador.Lineas.Should().Equal(string.Empty);
    }

    [Fact]
    public void El_ejemplo_del_documento_da_diez_apuestas_de_mil()
    {
        var borrador = Borrador(10_000, "1234", "4323", "5676", "2398", "9878");

        var calculo = borrador.Calcular();

        calculo.Should().Be(new CalculoJuegoNumerosManual(5, 2, 10, 1_000, 0));
        borrador.Validar([]).Should().BeNull();
    }

    [Fact]
    public void El_calculo_solo_cuenta_numeros_validos_y_se_actualiza_al_cambiar_loterias()
    {
        var borrador = Borrador(10_000, "1234", "12", "5676");

        borrador.Calcular().CantidadNumeros.Should().Be(2);

        borrador.Seleccionar(Medellin, "Medellín", false);

        borrador.Calcular().Should().Be(new CalculoJuegoNumerosManual(2, 1, 2, 5_000, 0));
    }

    [Fact]
    public void Una_division_no_exacta_reporta_las_apuestas_con_sobrante()
    {
        var borrador = Borrador(10_000, "111", "222", "333");

        borrador.Calcular().Should().Be(new CalculoJuegoNumerosManual(3, 2, 6, 1_666, 4));
    }

    [Theory]
    [InlineData(0, new[] { "1234" }, JuegoNumerosManualMessages.ValorRequerido)]
    [InlineData(10_000, new string[0], JuegoNumerosManualMessages.NumerosRequeridos)]
    public void Impide_registrar_jugadas_incompletas(int valor, string[] numeros, string esperado)
    {
        Borrador(valor, numeros).Validar([]).Should().Be(esperado);
    }

    [Fact]
    public void Exige_al_menos_una_loteria()
    {
        var borrador = Borrador(10_000, "1234");
        borrador.Seleccionar(Chontico, "Chontico", false);
        borrador.Seleccionar(Medellin, "Medellín", false);

        borrador.Validar([]).Should().Be(JuegoNumerosManualMessages.LoteriasRequeridas);
    }

    [Fact]
    public void Bloquea_numeros_invalidos_repetidos_y_restringidos()
    {
        Borrador(10_000, "1234", "12").Validar([]).Should().Be(string.Format(JuegoNumerosManualMessages.NumeroFormato, "12"));
        Borrador(10_000, "1234", "1234").Validar([]).Should().Be(string.Format(JuegoNumerosManualMessages.NumeroRepetido, "1234"));
        Borrador(10_000, "1234").Validar(["1234"]).Should().Be(string.Format(JuegoNumerosManualMessages.NumeroRestringido, "1234"));
    }

    [Fact]
    public void Bloquea_un_valor_que_no_alcanza_un_peso_por_apuesta()
    {
        Borrador(3, "111", "222").Validar([]).Should().Be(string.Format(JuegoNumerosManualMessages.ValorInsuficiente, 4));
    }

    [Fact]
    public void El_identificador_de_la_jugada_se_conserva_hasta_que_cambia_un_dato()
    {
        var borrador = Borrador(10_000, "1234");
        var primero = borrador.VentaId;

        borrador.VentaId.Should().Be(primero);

        borrador.CambiarValor(20_000);

        borrador.VentaId.Should().NotBe(primero);
    }

    [Fact]
    public void La_tirilla_usa_la_plantilla_con_jugadas_y_qr()
    {
        var recibo = ReciboJuegoNumerosManual.DesdeRespuesta(new JuegoNumerosManualResponse
        {
            CodigoPublico = "1234567",
            CodigoImpreso = "123-4567",
            Qr = "qr-del-boleto",
            FechaVenta = new DateTime(2026, 10, 16, 15, 5, 0),
            ValorJugado = 10_000m,
            CantidadNumeros = 5,
            CantidadLoterias = 2,
            CantidadApuestas = 10,
            ValorPorApuesta = 1_000m,
            Numeros = ["1234", "4323", "5676", "2398", "9878"],
            Loterias = ["Chontico", "Medellín"]
        }, "GRACIAS POR SU COMPRA.");

        var texto = recibo.Texto;
        var lineas = texto.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        recibo.Qr.Should().Be("qr-del-boleto");
        texto.Should().Contain("RECIBO DE VENTA");
        texto.Should().Contain("123-4567");
        texto.Should().Contain("Fecha: 2026-10-16");
        texto.Should().Contain("Hora: 15:05");
        texto.Should().Contain(TirillaJuegoNumerosManual.TipoApuesta);
        texto.Should().Contain("LOTERIAS: CHONTICO,");
        lineas.Should().Contain("MEDELLÍN");
        texto.Should().Contain("1234  4323  5676  2398");
        texto.Should().Contain("$1.000");
        texto.Should().Contain("$10.000");
        texto.Should().Contain(TirillaTexto.MarcaQr);
        texto.Should().Contain(JuegoNumerosManualTexts.MuchaSuerte);
        lineas.Should().OnlyContain(l => l.Length <= TirillaTexto.AnchoImpresora);
    }

    [Fact]
    public void Solo_la_falta_de_conexion_se_reintenta_y_un_rechazo_se_muestra()
    {
        JuegoNumerosManualApiClient.EsFalloDeConexion(JuegoNumerosManualApiClient.SinRed).Should().BeTrue();
        JuegoNumerosManualApiClient.EsFalloDeConexion(503).Should().BeTrue();
        JuegoNumerosManualApiClient.EsFalloDeConexion(400).Should().BeFalse();
        JuegoNumerosManualApiClient.EsFalloDeConexion(401).Should().BeFalse();
    }

    [Fact]
    public void Leer_distingue_rechazo_del_servidor_de_respuesta_ilegible()
    {
        JuegoNumerosManualApiClient.Leer("""{"success":false,"message":"cerrada"}""", 400).StatusCode.Should().Be(400);
        JuegoNumerosManualApiClient.Leer("<html>", 502).StatusCode.Should().Be(502);
        JuegoNumerosManualApiClient.Leer("<html>", 200).StatusCode.Should().Be(JuegoNumerosManualApiClient.SinRed);
        var ok = JuegoNumerosManualApiClient.Leer("""{"success":true,"message":"ok","data":{"codigoImpreso":"123-4567","qr":"qr-del-boleto","valorJugado":10000}}""", 201);
        ok.IsSuccess.Should().BeTrue();
        ok.Data!.CodigoImpreso.Should().Be("123-4567");
        ok.Data.Qr.Should().Be("qr-del-boleto");
    }
}
