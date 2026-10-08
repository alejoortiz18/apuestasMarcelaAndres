using FluentAssertions;

namespace NewRich.Admin.Tests;

public sealed class VentasPorBoletoVistaTests
{
    [Fact]
    public void La_fecha_del_boleto_se_muestra_con_la_hora_de_Colombia_guardada()
    {
        var vista = File.ReadAllText(RutaVista());

        vista.Should().Contain("@row.Fecha.ToString(\"dd/MM/yyyy HH:mm\")");
        vista.Should().NotContain("row.Fecha.ToLocalTime()");
    }

    private static string RutaVista() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..",
        "Administrador", "Views", "Ventas", "Index.cshtml"));
}
