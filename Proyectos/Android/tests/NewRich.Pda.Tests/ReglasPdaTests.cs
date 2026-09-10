using FluentAssertions;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Auth;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Pda.Tests;

public sealed class ReglasPdaTests
{
    [Fact]
    public void Tipo_combinado_se_muestra_como_combinada()
    {
        TipoApuestaEtiqueta.Texto(TipoApuesta.COMBINADO).Should().Be(PdaTexts.TipoCombinada);
        TipoApuestaEtiqueta.Texto(TipoApuesta.INDIVIDUAL).Should().Be(PdaTexts.TipoIndividual);
    }

    [Fact]
    public void Historico_acepta_hasta_diez_dias_atras()
    {
        var hoy = new DateTime(2026, 9, 9);

        HistoricoVentasReglas.FechaPermitida(hoy.AddDays(-10), hoy).Should().BeTrue();
        HistoricoVentasReglas.FechaPermitida(hoy.AddDays(-11), hoy).Should().BeFalse();
        HistoricoVentasReglas.FechaPermitida(hoy.AddDays(1), hoy).Should().BeFalse();
    }

    [Fact]
    public void Horario_cerrado_muestra_el_aviso_de_juegos_cerrados()
    {
        AuthPantalla.Mensaje(AuthMessages.FueraDeHorarioOperacion).Should().Be(PdaTexts.JuegosCerrados);
    }

    [Fact]
    public void Otros_mensajes_de_auth_se_muestran_tal_cual()
    {
        AuthPantalla.Mensaje(AuthMessages.CredencialesInvalidas).Should().Be(AuthMessages.CredencialesInvalidas);
    }

    [Fact]
    public void Administrador_no_puede_entrar_al_pda()
    {
        var resultado = NavegacionPorRol.Para(RolUsuario.Administrador);

        resultado.IsSuccess.Should().BeFalse();
        resultado.Message.Should().Be(AuthMessages.AdministradorNoOperaEnPda);
    }

    [Fact]
    public void Vendedor_abre_shell_vendedor()
    {
        NavegacionPorRol.Para(RolUsuario.Vendedor).Data.Should().Be(ShellPda.Vendedor);
    }

    [Fact]
    public void Observador_abre_shell_observador()
    {
        NavegacionPorRol.Para(RolUsuario.Observador).Data.Should().Be(ShellPda.Observador);
    }
}
