using NewRich.Application.Contracts.Configuracion;
using NewRich.Domain.Services;

namespace NewRich.Pda.Core.Auth;

/// <summary>Valida apertura/cierre guardados en el PDA (online u offline).</summary>
public static class HorarioPda
{
    public static bool EstaFuera(ConfiguracionOperativaResponse? limites, DateTime ahoraLocal)
    {
        if (limites is null)
        {
            return false;
        }

        if (!TimeSpan.TryParse(limites.HoraApertura, out var apertura)
            || !TimeSpan.TryParse(limites.HoraCierre, out var cierre))
        {
            return false;
        }

        return HorarioOperacion.EstaFuera(ahoraLocal.TimeOfDay, apertura, cierre);
    }

    /// <summary>Solo se puede iniciar un juego nuevo dentro del horario.</summary>
    public static bool PuedeIniciarJuegoNuevo(bool horarioCerrado) => !horarioCerrado;

    /// <summary>Si ya hay borrador (venta empezada), se puede terminar aunque haya cerrado.</summary>
    public static bool PuedeContinuarVentaActiva(bool horarioCerrado, bool tieneBorrador) =>
        !horarioCerrado || tieneBorrador;
}
