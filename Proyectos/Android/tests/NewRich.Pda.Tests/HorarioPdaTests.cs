using FluentAssertions;
using NewRich.Application.Contracts.Configuracion;
using NewRich.Pda.Core.Auth;

namespace NewRich.Pda.Tests;

public sealed class HorarioPdaTests
{
    [Fact]
    public void Antes_de_la_apertura_esta_fuera_de_horario()
    {
        var limites = new ConfiguracionOperativaResponse
        {
            HoraApertura = "10:00:00",
            HoraCierre = "20:00:00"
        };

        HorarioPda.EstaFuera(limites, DateTime.Today.AddHours(9).AddMinutes(59)).Should().BeTrue();
        HorarioPda.EstaFuera(limites, DateTime.Today.AddHours(10)).Should().BeTrue();
        HorarioPda.EstaFuera(limites, DateTime.Today.AddHours(10).AddSeconds(1)).Should().BeFalse();
    }

    [Fact]
    public void Despues_del_cierre_no_inicia_juego_pero_si_puede_terminar_venta_activa()
    {
        HorarioPda.PuedeIniciarJuegoNuevo(horarioCerrado: true).Should().BeFalse();
        HorarioPda.PuedeContinuarVentaActiva(horarioCerrado: true, tieneBorrador: true).Should().BeTrue();
        HorarioPda.PuedeContinuarVentaActiva(horarioCerrado: true, tieneBorrador: false).Should().BeFalse();
    }
}

public sealed class CredencialLocalTests
{
    [Fact]
    public void Guarda_hash_y_valida_usuario_y_contraseña()
    {
        var registro = CredencialLocal.Crear("vendedor1", "ClaveSegura1!");

        CredencialLocal.Coincide(registro, "vendedor1", "ClaveSegura1!").Should().BeTrue();
        CredencialLocal.Coincide(registro, "vendedor1", "otra").Should().BeFalse();
        CredencialLocal.Coincide(registro, "otro", "ClaveSegura1!").Should().BeFalse();
        registro.Hash.Should().NotBe("ClaveSegura1!");
    }
}
