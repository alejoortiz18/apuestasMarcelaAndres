using FluentAssertions;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;
using NewRich.Constants;

namespace NewRich.UnitTests;

public sealed class ConfirmacionAccionMemoriaTests
{
    [Fact]
    public void Publicar_la_aplicacion_sigue_confirmado_despues_de_dos_minutos()
    {
        var reloj = new Reloj(new DateTime(2026, 9, 29, 23, 0, 0, DateTimeKind.Utc));
        var store = new ConfirmacionAccionMemoria(reloj);
        var usuarioId = Guid.NewGuid();
        var token = store.Emitir(
            usuarioId,
            AccionesProtegidas.ConfiguracionVersionAplicacion,
            vigencia: ConfirmacionAccion.VigenciaCargaArchivo);

        reloj.Avanzar(TimeSpan.FromMinutes(3));

        store.Consumir(token, usuarioId, AccionesProtegidas.ConfiguracionVersionAplicacion).Should().BeTrue();
    }

    [Fact]
    public void Una_confirmacion_normal_caduca_a_los_dos_minutos()
    {
        var reloj = new Reloj(new DateTime(2026, 9, 29, 23, 0, 0, DateTimeKind.Utc));
        var store = new ConfirmacionAccionMemoria(reloj);
        var usuarioId = Guid.NewGuid();
        var token = store.Emitir(usuarioId, AccionesProtegidas.UsuariosEliminar);

        reloj.Avanzar(ConfirmacionAccion.Vigencia + TimeSpan.FromSeconds(1));

        store.Consumir(token, usuarioId, AccionesProtegidas.UsuariosEliminar).Should().BeFalse();
    }

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

    private sealed class Reloj : IClock
    {
        public Reloj(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; private set; }
        public DateTime LocalNow => UtcNow;
        public void Avanzar(TimeSpan cuanto) => UtcNow = UtcNow.Add(cuanto);
    }
}
