using FluentAssertions;
using NewRich.Application.Contracts.Android;
using NewRich.Constants.Messages;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class DescargaCodigosOfflineTests
{
    [Fact]
    public void Sin_codigos_muestra_que_no_hay_disponibles()
    {
        DescargaCodigosOffline.Mensaje([]).Should().Be(UsuarioMessages.SinCodigosOfflineDisponibles);
        DescargaCodigosOffline.HayCodigos([]).Should().BeFalse();
        DescargaCodigosOffline.HayCodigos(null).Should().BeFalse();
    }

    [Fact]
    public void Con_codigos_confirma_la_descarga()
    {
        var data = new[]
        {
            new CodigoOfflineAndroidResponse { Consecutivo = "OFF-000001", PayloadBase64 = "abc" }
        };

        DescargaCodigosOffline.HayCodigos(data).Should().BeTrue();
        DescargaCodigosOffline.Mensaje(data).Should().Be(SuccessMessages.CodigosOfflineDescargados);
    }
}
