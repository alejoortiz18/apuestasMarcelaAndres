using FluentAssertions;
using NewRich.Shared;

namespace NewRich.UnitTests;

public sealed class CorsOrigenesTests
{
    [Fact]
    public void ConLoopback_incluye_127_cuando_esta_localhost()
    {
        var origenes = CorsOrigenes.ConLoopback(["http://localhost:5274"]);

        origenes.Should().Contain("http://localhost:5274");
        origenes.Should().Contain("http://127.0.0.1:5274");
    }

    [Fact]
    public void EsPermitido_acepta_el_origen_del_navegador_en_127()
    {
        CorsOrigenes.EsPermitido("http://127.0.0.1:5274", ["http://localhost:5274"]).Should().BeTrue();
        CorsOrigenes.EsPermitido("http://evil.example:5274", ["http://localhost:5274"]).Should().BeFalse();
    }
}
