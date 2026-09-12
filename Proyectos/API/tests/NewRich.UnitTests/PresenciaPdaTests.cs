using FluentAssertions;
using NewRich.Shared;

namespace NewRich.UnitTests;

public sealed class PresenciaPdaTests
{
    private static readonly DateTime Ahora = new(2026, 9, 11, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Sin_conexion_ni_pulso_esta_desconectado()
    {
        PresenciaPda.EstaConectado(0, null, Ahora).Should().BeFalse();
    }

    [Fact]
    public void Con_conexion_viva_esta_conectado()
    {
        PresenciaPda.EstaConectado(1, null, Ahora).Should().BeTrue();
    }

    [Fact]
    public void Pulso_reciente_esta_conectado()
    {
        PresenciaPda.EstaConectado(0, Ahora.AddSeconds(-20), Ahora).Should().BeTrue();
    }

    [Fact]
    public void Pulso_viejo_esta_desconectado()
    {
        PresenciaPda.EstaConectado(0, Ahora.AddMinutes(-10), Ahora).Should().BeFalse();
    }
}
