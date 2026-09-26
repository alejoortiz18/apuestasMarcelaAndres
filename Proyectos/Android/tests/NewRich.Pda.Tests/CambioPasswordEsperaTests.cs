using FluentAssertions;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Auth;

namespace NewRich.Pda.Tests;

public class CambioPasswordEsperaTests
{
    [Fact]
    public void Con_conexion_la_validacion_tiene_tope_corto_y_entra_sin_esperar_la_sincronizacion()
    {
        CambioPasswordEspera.Segundos.Should().BeInRange(4, 10);
        CambioPasswordEspera.EsperarSincronizacionAntesDeEntrar.Should().BeFalse();
        PdaTexts.GuardandoContrasena.Should().Be("Guardando la contraseña...");
    }
}
