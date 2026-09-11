using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public class LoginPantallaTextsTests
{
    [Fact]
    public void El_inicio_de_sesion_usa_los_textos_del_acceso_asignado()
    {
        PdaTexts.IniciarSesion.Should().Be("Iniciar sesión");
        PdaTexts.LoginAyuda.Should().Be("Ingresa con tu usuario y contraseña asignados por el administrador.");
        PdaTexts.Ingresar.Should().Be("Ingresar");
        PdaTexts.IniciandoSesion.Should().Be("Iniciando sesión...");
        PdaTexts.Cargando.Should().Be("Cargando...");
        PdaTexts.MostrarContrasena.Should().Be("Mostrar contraseña");
        PdaTexts.OcultarContrasena.Should().Be("Ocultar contraseña");
    }
}
