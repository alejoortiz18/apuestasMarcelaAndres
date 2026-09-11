using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class CodigosOfflineEnDispositivoTests
{
    [Fact]
    public void Linea_distingue_disponible_y_usado()
    {
        CodigosOfflineEnDispositivo.Linea("OFF-000001", false)
            .Should().Be("OFF-000001 · Disponible");
        CodigosOfflineEnDispositivo.Linea("OFF-000002", true)
            .Should().Be("OFF-000002 · Usado");
    }
}
