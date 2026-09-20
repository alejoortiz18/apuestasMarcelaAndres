using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class InactividadPdaTests
{
    private static readonly DateTime Ahora = new(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PuedeEliminar_rechaza_actividad_dentro_del_umbral_configurado()
    {
        InactividadPda.PuedeEliminar(Ahora.AddDays(-6), Ahora, 7).Should().BeFalse();
    }

    [Fact]
    public void PuedeEliminar_permite_actividad_igual_al_umbral_configurado()
    {
        InactividadPda.PuedeEliminar(Ahora.AddDays(-7), Ahora, 7).Should().BeTrue();
    }

    [Fact]
    public void PuedeEliminar_usa_30_por_defecto_si_el_umbral_es_invalido()
    {
        InactividadPda.PuedeEliminar(Ahora.AddDays(-30), Ahora, 0).Should().BeTrue();
        InactividadPda.PuedeEliminar(Ahora.AddDays(-29), Ahora, -1).Should().BeFalse();
    }
}
