using FluentAssertions;
using NewRich.Infrastructure.Storage;

namespace NewRich.UnitTests;

public sealed class CargaBlobApkTests
{
    [Fact]
    public void Un_apk_grande_se_envia_en_bloques_con_tiempo_de_red_amplio()
    {
        var transferencia = CargaBlobApk.Transferencia();

        transferencia.MaximumTransferSize.Should().Be(4 * 1024 * 1024);
        transferencia.InitialTransferSize.Should().Be(4 * 1024 * 1024);
        CargaBlobApk.Opciones().Retry.NetworkTimeout.Should().BeGreaterThan(TimeSpan.FromMinutes(5));
    }
}
