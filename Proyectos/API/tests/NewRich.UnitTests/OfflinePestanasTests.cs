using FluentAssertions;
using NewRich.Application.Services;

namespace NewRich.UnitTests;

public sealed class OfflinePestanasTests
{
    [Fact]
    public void Pestana_desconocida_cae_en_informacion_general()
    {
        OfflinePestanas.Normalizar(null).Should().Be(OfflinePestanas.General);
        OfflinePestanas.Normalizar("otra").Should().Be(OfflinePestanas.General);
    }

    [Fact]
    public void Pestanas_de_generar_y_registrar_se_conservan()
    {
        OfflinePestanas.Normalizar(OfflinePestanas.Generar).Should().Be(OfflinePestanas.Generar);
        OfflinePestanas.Normalizar(OfflinePestanas.Registrar).Should().Be(OfflinePestanas.Registrar);
        OfflinePestanas.Es(OfflinePestanas.Generar, OfflinePestanas.Generar).Should().BeTrue();
        OfflinePestanas.Es(OfflinePestanas.General, OfflinePestanas.Registrar).Should().BeFalse();
    }
}
