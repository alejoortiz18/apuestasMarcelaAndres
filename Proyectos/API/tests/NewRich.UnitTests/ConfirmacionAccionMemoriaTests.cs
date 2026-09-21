using FluentAssertions;
using NewRich.Application.Services;
using NewRich.Constants;

namespace NewRich.UnitTests;

public sealed class ConfirmacionAccionMemoriaTests
{
    [Fact]
    public void Un_token_de_lote_sirve_tantas_veces_como_usos_y_luego_queda_invalido()
    {
        var store = new ConfirmacionAccionMemoria();
        var usuarioId = Guid.NewGuid();
        var token = store.Emitir(usuarioId, AccionesProtegidas.PdaBloquear, 3);

        store.Consumir(token, usuarioId, AccionesProtegidas.PdaBloquear).Should().BeTrue();
        store.Consumir(token, usuarioId, AccionesProtegidas.PdaBloquear).Should().BeTrue();
        store.Consumir(token, usuarioId, AccionesProtegidas.PdaBloquear).Should().BeTrue();
        store.Consumir(token, usuarioId, AccionesProtegidas.PdaBloquear).Should().BeFalse();
    }

    [Fact]
    public void Una_accion_distinta_no_consume_el_token()
    {
        var store = new ConfirmacionAccionMemoria();
        var usuarioId = Guid.NewGuid();
        var token = store.Emitir(usuarioId, AccionesProtegidas.UsuariosEliminar);

        store.Consumir(token, usuarioId, AccionesProtegidas.UsuariosCrear).Should().BeFalse();
        store.Consumir(token, usuarioId, AccionesProtegidas.UsuariosEliminar).Should().BeTrue();
    }
}
