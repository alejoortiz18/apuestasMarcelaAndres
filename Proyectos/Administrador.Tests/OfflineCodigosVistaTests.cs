using FluentAssertions;

namespace NewRich.Admin.Tests;

public sealed class OfflineCodigosVistaTests
{
    [Fact]
    public void Las_fechas_de_los_codigos_offline_se_muestran_en_hora_de_Colombia_sin_restar_horas()
    {
        var vista = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Administrador", "Views", "Offline", "Codigos.cshtml")));

        vista.Should().NotContain("ToLocalTime()");
        vista.Should().Contain("fecha.Value.ToString(\"dd/MM/yyyy HH:mm\")");
        vista.Should().Contain("data-fecha-creacion=\"@Formato(item.FechaCreacion)\"");
    }
}
