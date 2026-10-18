using FluentAssertions;
using NewRich.Admin.Constants;

namespace NewRich.Admin.Tests;

public sealed class LoginAyudaTests
{
    [Fact]
    public void El_login_del_administrador_muestra_solo_el_nombre_del_sitio()
    {
        UiTexts.LoginAyuda.Should().Be("Sitio administrador");

        var vista = File.ReadAllText(RutaVista());
        vista.Should().Contain("@UiTexts.LoginAyuda");
    }

    private static string RutaVista()
    {
        var ruta = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Administrador", "Views", "Cuenta", "Ingresar.cshtml"));
        File.Exists(ruta).Should().BeTrue($"se esperaba la vista en {ruta}");
        return ruta;
    }
}
