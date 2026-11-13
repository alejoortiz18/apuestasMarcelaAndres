using FluentAssertions;

namespace NewRich.Admin.Tests;

public sealed class MoverSeccionVistaTests
{
    [Fact]
    public void MoverSeccion_se_referencia_con_ruta_completa_para_abrirse_desde_utilidades()
    {
        var carpeta = Ruta("Administrador", "Views", "Informes");
        File.Exists(Path.Combine(carpeta, "_MoverSeccion.cshtml")).Should().BeTrue();
        foreach (var vista in Directory.GetFiles(carpeta, "*.cshtml"))
        {
            File.ReadAllText(vista).Should().NotContain("name=\"_MoverSeccion\"");
        }
    }

    private static string Ruta(params string[] partes) => Path.GetFullPath(Path.Combine(
        new[] { AppContext.BaseDirectory, "..", "..", "..", ".." }.Concat(partes).ToArray()));
}
