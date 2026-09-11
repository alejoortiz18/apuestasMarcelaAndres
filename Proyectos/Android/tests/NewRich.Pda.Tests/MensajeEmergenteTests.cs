using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class MensajeEmergenteTests
{
    [Fact]
    public void El_aviso_tiene_un_solo_boton_de_cierre()
    {
        var mensaje = MensajeEmergente.Aviso("Soporte", "No se pudo enviar", PdaTexts.Cerrar);

        mensaje.Titulo.Should().Be("Soporte");
        mensaje.Cuerpo.Should().Be("No se pudo enviar");
        mensaje.Principal.Should().Be(PdaTexts.Cerrar);
        mensaje.Secundario.Should().BeNull();
        mensaje.EsConfirmacion.Should().BeFalse();
    }

    [Fact]
    public void La_confirmacion_pone_el_no_a_la_izquierda_y_el_si_a_la_derecha()
    {
        var mensaje = MensajeEmergente.Confirmar(
            PdaTexts.CancelarBoleto,
            PdaTexts.CancelarBoletoConfirma,
            PdaTexts.SiCancelar,
            PdaTexts.No);

        mensaje.EsConfirmacion.Should().BeTrue();
        mensaje.Secundario.Should().Be(PdaTexts.No);
        mensaje.Principal.Should().Be(PdaTexts.SiCancelar);
    }
}
