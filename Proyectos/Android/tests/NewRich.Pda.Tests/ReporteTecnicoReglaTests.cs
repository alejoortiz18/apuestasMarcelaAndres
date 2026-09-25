using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class ReporteTecnicoReglaTests
{
    [Fact]
    public void Textos_del_flujo_reportar_usan_observacion()
    {
        PdaTexts.Reportar.Should().Be("Reportar");
        PdaTexts.ObservacionReporte.Should().Be("Observación");
        PdaTexts.EnviarReporte.Should().Be("Enviar");
        PdaTexts.CancelarReporte.Should().Be("Cancelar");
    }

    [Fact]
    public void ValidarObservacion_exige_detalle()
    {
        ReporteTecnicoRegla.ValidarObservacion(" ").Should().Be(PdaTexts.ObservacionReporteObligatoria);
        ReporteTecnicoRegla.ValidarObservacion("Falla en impresión").Should().BeNull();
    }

    [Fact]
    public void EstadoAlEncolar_muestra_esperando_sin_conexion()
    {
        ReporteTecnicoRegla.EstadoAlEncolar(hayConexion: false).Should().Be(EstadoReporteTecnico.EsperandoConexion);
        ReporteTecnicoRegla.EstadoAlEncolar(hayConexion: true).Should().Be(EstadoReporteTecnico.Enviado);
    }

    [Fact]
    public void DebeEnviarPendientes_solo_con_conexion_y_vendedor()
    {
        ReporteTecnicoRegla.DebeEnviarPendientes(true, NewRich.Domain.Enums.RolUsuario.Vendedor).Should().BeTrue();
        ReporteTecnicoRegla.DebeEnviarPendientes(false, NewRich.Domain.Enums.RolUsuario.Vendedor).Should().BeFalse();
        ReporteTecnicoRegla.DebeEnviarPendientes(true, NewRich.Domain.Enums.RolUsuario.Observador).Should().BeFalse();
        ReporteTecnicoRegla.DebeEnviarPendientes(true, NewRich.Domain.Enums.RolUsuario.Vendedor, debeCambiarPassword: true).Should().BeTrue();
    }
}
