using NewRich.Domain.Enums;

namespace NewRich.Pda.Core;

public static class EstadoReporteTecnico
{
    public const string EsperandoConexion = "Enviado, esperando conexión";
    public const string Enviado = "Enviado";
}

public static class ReporteTecnicoRegla
{
    public static string? ValidarObservacion(string? observacion)
    {
        if (string.IsNullOrWhiteSpace(observacion))
        {
            return PdaTexts.ObservacionReporteObligatoria;
        }

        return null;
    }

    public static string EstadoAlEncolar(bool hayConexion) =>
        hayConexion ? EstadoReporteTecnico.Enviado : EstadoReporteTecnico.EsperandoConexion;

    /// <summary>
    /// Los reportes técnicos deben salir al recuperar conexión aunque el vendedor deba cambiar contraseña.
    /// </summary>
    public static bool DebeEnviarPendientes(bool conectado, RolUsuario rol, bool debeCambiarPassword = false) =>
        conectado && rol == RolUsuario.Vendedor;
}
