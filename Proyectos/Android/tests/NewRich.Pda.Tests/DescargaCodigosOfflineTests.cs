using FluentAssertions;
using NewRich.Application.Contracts.Android;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
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

    [Fact]
    public void Vendedor_conectado_descarga_en_silencio_al_iniciar_sesion()
    {
        DescargaCodigosOffline.SincronizarEnSilencio(true, RolUsuario.Vendedor, false).Should().BeTrue();
    }

    [Fact]
    public void No_descarga_en_silencio_si_no_hay_conexion_o_no_es_vendedor()
    {
        DescargaCodigosOffline.SincronizarEnSilencio(false, RolUsuario.Vendedor, false).Should().BeFalse();
        DescargaCodigosOffline.SincronizarEnSilencio(true, RolUsuario.Observador, false).Should().BeFalse();
        DescargaCodigosOffline.SincronizarEnSilencio(true, RolUsuario.Vendedor, true).Should().BeFalse();
    }

    [Fact]
    public void El_aviso_en_vivo_solo_aplica_al_pda_asignado()
    {
        var pda = Guid.NewGuid();
        DescargaCodigosOffline.AceptaAvisoEnVivo(pda, pda).Should().BeTrue();
        DescargaCodigosOffline.AceptaAvisoEnVivo(pda, Guid.NewGuid()).Should().BeFalse();
        DescargaCodigosOffline.AceptaAvisoEnVivo(null, pda).Should().BeFalse();
    }

    [Fact]
    public void ParaGuardar_omite_respuestas_vacias()
    {
        DescargaCodigosOffline.ParaGuardar(null).Should().BeEmpty();
        DescargaCodigosOffline.ParaGuardar([]).Should().BeEmpty();
        DescargaCodigosOffline.ParaGuardar(
        [
            new CodigoOfflineAndroidResponse { Consecutivo = "OFF-000003", PayloadBase64 = "xyz" }
        ]).Should().Equal(("OFF-000003", "xyz"));
    }
}
