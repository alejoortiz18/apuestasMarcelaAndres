using FluentAssertions;
using NewRich.Pda.Core.Auth;

namespace NewRich.Pda.Tests;

public sealed class RelojInactividadTests
{
    private static readonly DateTime Inicio = new(2026, 9, 26, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Cierra_la_sesion_al_cumplir_un_minuto_sin_actividad()
    {
        var reloj = new RelojInactividad();
        reloj.Iniciar(Inicio);

        reloj.DebeCerrar(Inicio.AddSeconds(59)).Should().BeFalse();
        reloj.DebeCerrar(Inicio.AddMinutes(1)).Should().BeTrue();
    }

    [Fact]
    public void Un_toque_reinicia_el_minuto()
    {
        var reloj = new RelojInactividad();
        reloj.Iniciar(Inicio);
        var toque = Inicio.AddSeconds(50);
        reloj.RegistrarActividad(toque);

        reloj.DebeCerrar(toque.AddSeconds(59)).Should().BeFalse();
        reloj.DebeCerrar(toque.AddMinutes(1)).Should().BeTrue();
    }

    [Fact]
    public void Sin_sesion_no_cierra_y_un_toque_no_la_abre()
    {
        var reloj = new RelojInactividad();

        reloj.RegistrarActividad(Inicio);
        reloj.DebeCerrar(Inicio.AddMinutes(5)).Should().BeFalse();

        reloj.Iniciar(Inicio);
        reloj.Detener();
        reloj.DebeCerrar(Inicio.AddMinutes(5)).Should().BeFalse();
    }

    [Fact]
    public void Respeta_los_minutos_configurados_en_el_administrador()
    {
        var reloj = new RelojInactividad();
        reloj.Iniciar(Inicio, 3);

        reloj.DebeCerrar(Inicio.AddMinutes(1)).Should().BeFalse();
        reloj.DebeCerrar(Inicio.AddMinutes(3)).Should().BeTrue();
    }
}
