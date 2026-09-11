using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public class PasswordPantallaTextsTests
{
    [Fact]
    public void El_primer_acceso_pide_contrasena_definitiva_con_los_textos_de_la_pantalla()
    {
        PdaTexts.PrimerAcceso.Should().Be("PRIMER ACCESO");
        PdaTexts.EstablecerNuevaContrasena.Should().Be("Establecer nueva contraseña");
        PdaTexts.PrimerAccesoAyuda.Should().Be("Por seguridad debes definir una contraseña definitiva antes de operar el PDA.");
        PdaTexts.PlaceholderContrasenaActual.Should().Be("Ingresa tu contraseña actual");
        PdaTexts.PlaceholderNuevaContrasena.Should().Be("Ingresa tu nueva contraseña");
        PdaTexts.PlaceholderConfirmarContrasena.Should().Be("Confirma tu nueva contraseña");
        PdaTexts.GuardarYContinuar.Should().Be("Guardar y continuar");
        PdaTexts.AccesoPie.Should().Be("CONFIANZA  •  ESTRATEGIA  •  GRANDES RESULTADOS");
        PdaTexts.Volver.Should().Be("Volver");
    }
}
