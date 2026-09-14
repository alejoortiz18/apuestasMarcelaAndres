using FluentAssertions;
using NewRich.Domain.Services;

namespace NewRich.UnitTests;

public sealed class ZonaHorariaColombiaTests
{
    [Fact]
    public void Una_hora_utc_de_la_madrugada_sigue_siendo_la_noche_anterior_en_colombia()
    {
        var utc = new DateTime(2026, 9, 14, 1, 10, 7, DateTimeKind.Utc);

        var local = ZonaHorariaColombia.ALocal(utc);

        local.Should().Be(new DateTime(2026, 9, 13, 20, 10, 7));
    }
}
