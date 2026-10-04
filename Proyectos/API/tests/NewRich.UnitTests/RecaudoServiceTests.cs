using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Services;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Domain.Services;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class RecaudoServiceTests
{
    [Fact]
    public async Task Configuracion_marca_en_gris_un_grupo_sin_asignar()
    {
        var (sut, db) = Crear();
        await AgregarGrupo(db, "Norte");

        var config = await sut.ConfiguracionAsync(new DateOnly(2026, 9, 28), new DateOnly(2026, 9, 28), CancellationToken.None);

        config.IsSuccess.Should().BeTrue();
        config.Data!.Alarmas.Should().Contain("Norte");
        config.Data.Grupos.Should().ContainSingle(g => g.Nombre == "Norte" && g.SinConfigurar && g.Porcentaje == 0);
    }

    [Fact]
    public async Task Asignar_grupo_aparece_en_el_panel_y_se_puede_retirar()
    {
        var (sut, db) = Crear();
        var grupo = await AgregarGrupo(db, "Sur");
        var recaudador = await AgregarUsuario(db, "Carmen Recaudo", RolUsuario.Recaudador);
        var hoy = new DateOnly(2026, 9, 28);

        var asignado = await sut.AsignarGrupoAsync(new AsignarGrupoRecaudoRequest
        {
            RecaudadorId = recaudador.UsuarioId,
            GrupoId = grupo.GrupoId,
            Porcentaje = 80
        }, recaudador.UsuarioId, CancellationToken.None);

        asignado.IsSuccess.Should().BeTrue();
        var panel = await sut.PanelAsync(hoy, hoy, CancellationToken.None);
        panel.Data.Should().ContainSingle(p => p.Nombre == "Carmen Recaudo" && p.Grupos == "Sur" && p.PorcentajeRecaudado == 0);

        var retiro = await sut.RetirarGrupoAsync(grupo.GrupoId, CancellationToken.None);
        retiro.IsSuccess.Should().BeTrue();
        var luego = await sut.ConfiguracionAsync(hoy, hoy, CancellationToken.None);
        luego.Data!.Grupos.Should().ContainSingle(g => g.Nombre == "Sur" && g.SinConfigurar);
    }

    [Fact]
    public async Task Asignar_grupo_retira_la_asignacion_individual_del_miembro_con_el_mismo_recaudador()
    {
        var (sut, db) = Crear();
        var grupo = await AgregarGrupo(db, "Centro");
        var recaudador = await AgregarUsuario(db, "Carmen Recaudo", RolUsuario.Recaudador);
        var cata = await AgregarUsuario(db, "Cata Lopez", RolUsuario.Vendedor);
        db.AsignacionesVendedorRecaudo.Add(new AsignacionVendedorRecaudo
        {
            AsignacionId = Guid.NewGuid(),
            RecaudadorId = recaudador.UsuarioId,
            VendedorId = cata.UsuarioId,
            Porcentaje = 15,
            Estado = "Activa",
            FechaCreacion = DateTime.UtcNow,
            FechaModificacion = DateTime.UtcNow
        });
        db.UsuariosGrupos.Add(new UsuarioGrupo { UsuarioId = cata.UsuarioId, GrupoId = grupo.GrupoId });
        await db.SaveChangesAsync();

        var asignado = await sut.AsignarGrupoAsync(new AsignarGrupoRecaudoRequest
        {
            RecaudadorId = recaudador.UsuarioId,
            GrupoId = grupo.GrupoId,
            Porcentaje = 10
        }, recaudador.UsuarioId, CancellationToken.None);

        asignado.IsSuccess.Should().BeTrue(asignado.Message);
        db.AsignacionesVendedorRecaudo.Should().OnlyContain(a => a.Estado != "Activa");
        var hoy = new DateOnly(2026, 9, 28);
        var config = await sut.ConfiguracionAsync(hoy, hoy, CancellationToken.None);
        config.Data!.Grupos.Should().ContainSingle(g => g.Nombre == "Centro" && g.RecaudadorNombre == "Carmen Recaudo" && g.Porcentaje == 10);
    }

    [Fact]
    public async Task Una_venta_del_vendedor_asignado_genera_la_obligacion_del_dia()
    {
        var (sut, db) = Crear();
        var grupo = await AgregarGrupo(db, "Centro");
        var recaudador = await AgregarUsuario(db, "Carmen Recaudo", RolUsuario.Recaudador);
        var vendedor = await AgregarUsuario(db, "Ana Vende", RolUsuario.Vendedor);
        db.UsuariosGrupos.Add(new UsuarioGrupo { UsuarioId = vendedor.UsuarioId, GrupoId = grupo.GrupoId });
        db.Ventas.Add(new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = vendedor.UsuarioId,
            FechaVenta = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc),
            Total = 1005m,
            TipoApuesta = TipoApuesta.INDIVIDUAL,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        await db.SaveChangesAsync();
        await sut.AsignarGrupoAsync(new AsignarGrupoRecaudoRequest
        {
            RecaudadorId = recaudador.UsuarioId,
            GrupoId = grupo.GrupoId,
            Porcentaje = 10
        }, recaudador.UsuarioId, CancellationToken.None);

        var generado = await sut.GenerarDesdeVentaAsync(vendedor.UsuarioId, CancellationToken.None);
        var lista = await sut.ObligacionesAsync(recaudador.UsuarioId, new DateOnly(2026, 9, 28), CancellationToken.None);

        generado.IsSuccess.Should().BeTrue();
        lista.Data.Should().ContainSingle(o => o.VendedorId == vendedor.UsuarioId && o.ValorACobrar == 101m && o.TotalVendido == 1005m);
    }

    [Fact]
    public async Task La_obligacion_trae_usuario_y_documento_del_vendedor()
    {
        var (sut, db) = Crear();
        var grupo = await AgregarGrupo(db, "Centro");
        var recaudador = await AgregarUsuario(db, "Carmen Recaudo", RolUsuario.Recaudador);
        var vendedor = await AgregarUsuario(db, "Ana Vende", RolUsuario.Vendedor);
        vendedor.Documento = "1098000111";
        db.UsuariosGrupos.Add(new UsuarioGrupo { UsuarioId = vendedor.UsuarioId, GrupoId = grupo.GrupoId });
        db.Ventas.Add(new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = vendedor.UsuarioId,
            FechaVenta = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc),
            Total = 1000m,
            TipoApuesta = TipoApuesta.INDIVIDUAL,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        await db.SaveChangesAsync();
        await sut.AsignarGrupoAsync(new AsignarGrupoRecaudoRequest
        {
            RecaudadorId = recaudador.UsuarioId,
            GrupoId = grupo.GrupoId,
            Porcentaje = 10
        }, recaudador.UsuarioId, CancellationToken.None);

        var lista = await sut.ObligacionesAsync(recaudador.UsuarioId, new DateOnly(2026, 9, 28), CancellationToken.None);

        lista.Data.Should().ContainSingle(o => o.Usuario == "ana.vende" && o.Documento == "1098000111");
    }

    [Fact]
    public async Task El_pago_se_guarda_en_hora_de_colombia()
    {
        var utc = new DateTime(2026, 9, 28, 15, 0, 0, DateTimeKind.Utc);
        var (sut, db, recaudador, vendedor) = await PrepararVendedorConVentaAsync(utc);

        var resultado = await sut.RegistrarPagoAsync(recaudador.UsuarioId, new RegistrarPagoRecaudoRequest
        {
            VendedorId = vendedor.UsuarioId,
            Valor = 50m,
            ClaveIdempotencia = "clave-colombia"
        }, CancellationToken.None);

        var colombia = ZonaHorariaColombia.ALocal(utc);
        resultado.IsSuccess.Should().BeTrue(resultado.Message);
        resultado.Data!.FechaHora.Should().Be(colombia);
        db.PagosRecaudo.Should().ContainSingle(p => p.FechaHora == colombia);
        db.TirillasCobroRecaudo.Should().ContainSingle(t => t.FechaHora == colombia);
    }

    [Fact]
    public async Task Una_venta_de_la_madrugada_en_colombia_entra_en_la_obligacion_de_ese_dia()
    {
        var utc = new DateTime(2026, 9, 29, 15, 0, 0, DateTimeKind.Utc);
        var (sut, _, recaudador, vendedor) = await PrepararVendedorConVentaAsync(
            utc,
            new DateTime(2026, 9, 29, 0, 30, 0, DateTimeKind.Unspecified));

        var lista = await sut.ObligacionesAsync(recaudador.UsuarioId, new DateOnly(2026, 9, 29), CancellationToken.None);

        lista.Data.Should().ContainSingle(o => o.VendedorId == vendedor.UsuarioId && o.TotalVendido == 1000m);
    }

    [Fact]
    public async Task Un_pago_de_la_madrugada_en_colombia_queda_en_ese_dia()
    {
        var utc = new DateTime(2026, 9, 29, 6, 0, 0, DateTimeKind.Utc);
        var (sut, _, recaudador, vendedor) = await PrepararVendedorConVentaAsync(
            utc,
            new DateTime(2026, 9, 29, 5, 30, 0, DateTimeKind.Utc));

        await sut.RegistrarPagoAsync(recaudador.UsuarioId, new RegistrarPagoRecaudoRequest
        {
            VendedorId = vendedor.UsuarioId,
            Valor = 50m,
            ClaveIdempotencia = "clave-madrugada"
        }, CancellationToken.None);

        var del29 = await sut.ObligacionesAsync(recaudador.UsuarioId, new DateOnly(2026, 9, 29), CancellationToken.None);
        var del28 = await sut.ObligacionesAsync(recaudador.UsuarioId, new DateOnly(2026, 9, 28), CancellationToken.None);

        del29.Data.Should().ContainSingle(o => o.VendedorId == vendedor.UsuarioId && o.PagosHoy == 50m);
        (del28.Data ?? []).Should().NotContain(o => o.VendedorId == vendedor.UsuarioId && o.PagosHoy == 50m);
    }

    [Fact]
    public async Task Sin_cobro_lo_generado_hoy_queda_como_pendiente_del_dia()
    {
        var (sut, _, recaudador, vendedor) = await PrepararVendedorConVentaAsync(new DateTime(2026, 9, 28, 20, 0, 0, DateTimeKind.Utc));

        var lista = await sut.ObligacionesAsync(recaudador.UsuarioId, new DateOnly(2026, 9, 28), CancellationToken.None);

        var fila = lista.Data!.Single(o => o.VendedorId == vendedor.UsuarioId);
        fila.PendienteDelDia.Should().Be(100m);
        fila.TotalPendiente.Should().Be(100m);
    }

    [Fact]
    public async Task Tras_el_cobro_lo_que_queda_del_dia_ya_no_es_pendiente_del_dia()
    {
        var (sut, _, recaudador, vendedor) = await PrepararVendedorConVentaAsync(new DateTime(2026, 9, 28, 20, 0, 0, DateTimeKind.Utc));
        await sut.RegistrarPagoAsync(recaudador.UsuarioId, new RegistrarPagoRecaudoRequest
        {
            VendedorId = vendedor.UsuarioId,
            Valor = 60m,
            ClaveIdempotencia = "cobro-parcial"
        }, CancellationToken.None);

        var lista = await sut.ObligacionesAsync(recaudador.UsuarioId, new DateOnly(2026, 9, 28), CancellationToken.None);

        var fila = lista.Data!.Single(o => o.VendedorId == vendedor.UsuarioId);
        fila.PendienteDelDia.Should().Be(0m);
        fila.TotalPendiente.Should().Be(40m);
    }

    [Fact]
    public async Task Lo_vendido_despues_del_cobro_queda_como_pendiente_del_dia_y_sigue_cobrado()
    {
        var (sut, db, recaudador, vendedor) = await PrepararVendedorConVentaAsync(new DateTime(2026, 9, 28, 20, 0, 0, DateTimeKind.Utc));
        await sut.RegistrarPagoAsync(recaudador.UsuarioId, new RegistrarPagoRecaudoRequest
        {
            VendedorId = vendedor.UsuarioId,
            Valor = 60m,
            ClaveIdempotencia = "cobro-antes-de-vender"
        }, CancellationToken.None);
        db.Ventas.Add(new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = vendedor.UsuarioId,
            FechaVenta = new DateTime(2026, 9, 28, 17, 0, 0, DateTimeKind.Unspecified),
            Total = 500m,
            TipoApuesta = TipoApuesta.INDIVIDUAL,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        await db.SaveChangesAsync();

        var lista = await sut.ObligacionesAsync(recaudador.UsuarioId, new DateOnly(2026, 9, 28), CancellationToken.None);

        var fila = lista.Data!.Single(o => o.VendedorId == vendedor.UsuarioId);
        fila.ValorACobrar.Should().Be(150m);
        fila.PendienteDelDia.Should().Be(50m);
        fila.TotalPendiente.Should().Be(90m);
        fila.Lista.Should().Be(nameof(ListaCobro.Cobrados));
    }

    [Fact]
    public async Task Un_cobro_hecho_sin_conexion_queda_en_el_dia_y_hora_en_que_se_cobro()
    {
        var (sut, db, recaudador, vendedor) = await PrepararVendedorConVentaAsync(
            new DateTime(2026, 9, 29, 14, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Unspecified));

        var pago = await sut.RegistrarPagoAsync(recaudador.UsuarioId, new RegistrarPagoRecaudoRequest
        {
            VendedorId = vendedor.UsuarioId,
            Valor = 50m,
            ClaveIdempotencia = "cobro-sin-senal",
            FechaHoraCobro = new DateTime(2026, 9, 29, 4, 50, 0, DateTimeKind.Utc)
        }, CancellationToken.None);

        pago.IsSuccess.Should().BeTrue();
        pago.Data!.FechaHora.Should().Be(new DateTime(2026, 9, 28, 23, 50, 0));
        db.PagosRecaudo.Single().FechaHora.Should().Be(new DateTime(2026, 9, 28, 23, 50, 0));
        var del28 = await sut.ObligacionesAsync(recaudador.UsuarioId, new DateOnly(2026, 9, 28), CancellationToken.None);
        del28.Data!.Single(o => o.VendedorId == vendedor.UsuarioId).PagosHoy.Should().Be(50m);
        var del29 = await sut.ObligacionesAsync(recaudador.UsuarioId, new DateOnly(2026, 9, 29), CancellationToken.None);
        del29.Data!.Single(o => o.VendedorId == vendedor.UsuarioId).PagosHoy.Should().Be(0m);
    }

    [Fact]
    public async Task Un_cobro_con_hora_futura_queda_con_la_hora_del_servidor()
    {
        var (sut, db, recaudador, vendedor) = await PrepararVendedorConVentaAsync(new DateTime(2026, 9, 28, 20, 0, 0, DateTimeKind.Utc));

        await sut.RegistrarPagoAsync(recaudador.UsuarioId, new RegistrarPagoRecaudoRequest
        {
            VendedorId = vendedor.UsuarioId,
            Valor = 50m,
            ClaveIdempotencia = "cobro-futuro",
            FechaHoraCobro = new DateTime(2026, 9, 29, 20, 0, 0, DateTimeKind.Utc)
        }, CancellationToken.None);

        db.PagosRecaudo.Single().FechaHora.Should().Be(new DateTime(2026, 9, 28, 15, 0, 0));
    }

    [Fact]
    public async Task El_detalle_trae_la_ficha_del_recaudador_sus_grupos_y_la_hora_del_ultimo_pago()
    {
        var (sut, _, recaudador, vendedor) = await PrepararVendedorConVentaAsync(new DateTime(2026, 9, 28, 20, 0, 0, DateTimeKind.Utc));
        await sut.RegistrarPagoAsync(recaudador.UsuarioId, new RegistrarPagoRecaudoRequest
        {
            VendedorId = vendedor.UsuarioId,
            Valor = 60m,
            ClaveIdempotencia = "pago-detalle"
        }, CancellationToken.None);
        var hoy = new DateOnly(2026, 9, 28);

        var detalle = await sut.DetalleAsync(recaudador.UsuarioId, hoy, hoy, CancellationToken.None);

        detalle.IsSuccess.Should().BeTrue();
        detalle.Data!.Usuario.Should().Be("carmen.recaudo");
        var grupo = detalle.Data.Grupos.Should().ContainSingle().Subject;
        grupo.Nombre.Should().Be("Centro");
        grupo.Porcentaje.Should().Be(10);
        grupo.Vendedores.Should().Be(1);
        grupo.TotalPorRecaudar.Should().Be(100m);
        grupo.TotalRecaudado.Should().Be(60m);
        grupo.TotalPendiente.Should().Be(40m);
        grupo.PorcentajeRecaudado.Should().Be(60);
        detalle.Data.Vendedores.Single().UltimoPago.Should().Be(new DateTime(2026, 9, 28, 15, 0, 0));
    }

    [Fact]
    public async Task La_linea_de_tiempo_muestra_por_dia_lo_que_debian_y_lo_cobrado()
    {
        var (sut, db, recaudador, vendedor) = await PrepararVendedorConVentaAsync(new DateTime(2026, 9, 28, 20, 0, 0, DateTimeKind.Utc));
        db.Ventas.Add(new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = vendedor.UsuarioId,
            FechaVenta = new DateTime(2026, 9, 27, 10, 0, 0, DateTimeKind.Unspecified),
            Total = 500m,
            TipoApuesta = TipoApuesta.INDIVIDUAL,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        db.ObligacionesRecaudo.Add(new ObligacionRecaudo
        {
            ObligacionId = Guid.NewGuid(),
            VendedorId = vendedor.UsuarioId,
            RecaudadorId = recaudador.UsuarioId,
            Fecha = new DateTime(2026, 9, 27),
            TotalVendido = 500m,
            Porcentaje = 10,
            ValorGenerado = 50m,
            FechaGeneracion = new DateTime(2026, 9, 27, 10, 0, 0)
        });
        await db.SaveChangesAsync();
        await sut.RegistrarPagoAsync(recaudador.UsuarioId, new RegistrarPagoRecaudoRequest
        {
            VendedorId = vendedor.UsuarioId,
            Valor = 60m,
            ClaveIdempotencia = "pago-linea"
        }, CancellationToken.None);

        var linea = await sut.LineaDeTiempoAsync(recaudador.UsuarioId, new DateOnly(2026, 9, 26), new DateOnly(2026, 9, 28), CancellationToken.None);

        linea.IsSuccess.Should().BeTrue();
        linea.Data!.Select(d => d.Fecha).Should().Equal(new DateOnly(2026, 9, 26), new DateOnly(2026, 9, 27), new DateOnly(2026, 9, 28));
        linea.Data.Select(d => d.Debia).Should().Equal(0m, 50m, 150m);
        linea.Data.Select(d => d.Cobrado).Should().Equal(0m, 0m, 60m);
        linea.Data.Select(d => d.Generado).Should().Equal(0m, 50m, 100m);
        var centro = linea.Data[2].Grupos.Should().ContainSingle().Subject;
        centro.Grupo.Should().Be("Centro");
        centro.Debia.Should().Be(150m);
        centro.Cobrado.Should().Be(60m);
    }

    [Fact]
    public async Task El_porcentaje_cuenta_lo_cobrado_aunque_ese_dia_no_se_haya_generado_nada()
    {
        var (sut, _, recaudador, vendedor) = await DeudaDelDiaAnteriorCobradaHoyAsync();
        var hoy = new DateOnly(2026, 9, 28);

        var detalle = await sut.DetalleAsync(recaudador.UsuarioId, hoy, hoy, CancellationToken.None);

        detalle.Data!.TotalPorRecaudar.Should().Be(0m);
        detalle.Data.TotalRecaudado.Should().Be(60m);
        detalle.Data.SaldoPendiente.Should().Be(40m);
        detalle.Data.PorcentajeRecaudado.Should().Be(60);
        detalle.Data.Grupos.Single().PorcentajeRecaudado.Should().Be(60);
        detalle.Data.Vendedores.Should().ContainSingle(v => v.VendedorId == vendedor.UsuarioId);
    }

    [Fact]
    public async Task Las_tarjetas_del_detalle_suman_todo_el_periodo()
    {
        var (sut, _, recaudador, _) = await DeudaDelDiaAnteriorCobradaHoyAsync();

        var detalle = await sut.DetalleAsync(recaudador.UsuarioId, new DateOnly(2026, 9, 27), new DateOnly(2026, 9, 28), CancellationToken.None);

        detalle.Data!.TotalPorRecaudar.Should().Be(100m);
        detalle.Data.TotalRecaudado.Should().Be(60m);
        detalle.Data.SaldoPendiente.Should().Be(40m);
        detalle.Data.PorcentajeRecaudado.Should().Be(60);
        detalle.Data.LineaDeTiempo.Select(d => d.Fecha).Should().Equal(new DateOnly(2026, 9, 27), new DateOnly(2026, 9, 28));
        var grupo = detalle.Data.Grupos.Single();
        grupo.TotalPorRecaudar.Should().Be(100m);
        grupo.TotalRecaudado.Should().Be(60m);
        grupo.TotalPendiente.Should().Be(40m);
    }

    [Fact]
    public async Task El_panel_suma_el_periodo_y_usa_el_mismo_porcentaje_del_detalle()
    {
        var (sut, _, recaudador, _) = await DeudaDelDiaAnteriorCobradaHoyAsync();

        var panel = await sut.PanelAsync(new DateOnly(2026, 9, 27), new DateOnly(2026, 9, 28), CancellationToken.None);

        var fila = panel.Data!.Single(p => p.RecaudadorId == recaudador.UsuarioId);
        fila.TotalPorRecaudar.Should().Be(100m);
        fila.TotalRecaudado.Should().Be(60m);
        fila.SaldoPendiente.Should().Be(40m);
        fila.PorcentajeRecaudado.Should().Be(60);
    }

    [Fact]
    public async Task El_tablero_suma_toda_la_operacion_y_arma_las_barras_por_recaudador_y_grupo()
    {
        var (sut, _, carmen, rosa) = await DosRecaudadoresAsync();
        var hoy = new DateOnly(2026, 9, 28);

        var tablero = await sut.TableroAsync(new FiltroTableroRecaudo { Desde = hoy, Hasta = hoy }, CancellationToken.None);

        tablero.IsSuccess.Should().BeTrue();
        var t = tablero.Data!;
        t.TotalVendido.Should().Be(3000m);
        t.TotalPorRecaudar.Should().Be(300m);
        t.TotalRecaudado.Should().Be(60m);
        t.TotalPendiente.Should().Be(240m);
        t.PorcentajeRecaudo.Should().Be(20);
        t.DeudaAnterior.Should().Be(40m);
        t.PendienteDelDia.Should().Be(200m);
        (t.VendedoresAlDia + t.VendedoresPorCobrar + t.VendedoresEnDeuda).Should().Be(2);
        t.GruposConPendiente.Should().Be(2);
        t.Recaudadores.Select(r => (r.Nombre, r.TotalPorRecaudar, r.TotalRecaudado, r.TotalPendiente, r.PorcentajeRecaudo))
            .Should().Equal(("Carmen Recaudo", 100m, 60m, 40m, 60), ("Rosa Recaudo", 200m, 0m, 200m, 0));
        t.Grupos.Select(g => (g.Nombre, g.Detalle, g.TotalPendiente)).Should().Equal(("Centro", "Carmen Recaudo", 40m), ("Sur", "Rosa Recaudo", 200m));
        t.MayoresSaldos.Select(s => s.Nombre).Should().Equal("Beto Sur", "Ana Vende");
        t.OpcionesRecaudadores.Select(o => o.Id).Should().BeEquivalentTo([carmen.UsuarioId, rosa.UsuarioId]);
        t.OpcionesGrupos.Should().Equal("Centro", "Sur");
        t.Dias.Should().ContainSingle().Which.Cobrado.Should().Be(60m);
    }

    [Fact]
    public async Task El_tablero_filtrado_por_recaudador_solo_muestra_lo_suyo()
    {
        var (sut, _, carmen, _) = await DosRecaudadoresAsync();
        var hoy = new DateOnly(2026, 9, 28);

        var t = (await sut.TableroAsync(new FiltroTableroRecaudo { Desde = hoy, Hasta = hoy, RecaudadorId = carmen.UsuarioId }, CancellationToken.None)).Data!;

        t.TotalPorRecaudar.Should().Be(100m);
        t.TotalPendiente.Should().Be(40m);
        t.Recaudadores.Should().ContainSingle(r => r.Id == carmen.UsuarioId);
        t.Grupos.Select(g => g.Nombre).Should().Equal("Centro");
        t.OpcionesGrupos.Should().Equal("Centro", "Sur");
    }

    [Fact]
    public async Task El_tablero_filtrado_por_grupo_usa_los_dias_y_saldos_de_ese_grupo()
    {
        var (sut, _, _, rosa) = await DosRecaudadoresAsync();

        var t = (await sut.TableroAsync(new FiltroTableroRecaudo
        {
            Desde = new DateOnly(2026, 9, 27),
            Hasta = new DateOnly(2026, 9, 28),
            Grupo = "Sur"
        }, CancellationToken.None)).Data!;

        t.TotalVendido.Should().Be(2000m);
        t.TotalPorRecaudar.Should().Be(200m);
        t.TotalRecaudado.Should().Be(0m);
        t.Dias.Select(d => d.Debia).Should().Equal(0m, 200m);
        t.Recaudadores.Should().ContainSingle(r => r.Id == rosa.UsuarioId);
        t.MayoresSaldos.Select(s => s.Nombre).Should().Equal("Beto Sur");
    }

    [Fact]
    public async Task El_tablero_avisa_cuando_el_periodo_pasa_de_31_dias()
    {
        var (sut, _, _, _) = await DosRecaudadoresAsync();

        var t = (await sut.TableroAsync(new FiltroTableroRecaudo { Desde = new DateOnly(2026, 8, 1), Hasta = new DateOnly(2026, 9, 28) }, CancellationToken.None)).Data!;

        t.PeriodoRecortado.Should().BeTrue();
        t.Desde.Should().Be(new DateOnly(2026, 8, 29));
        t.Dias.Should().HaveCount(31);
    }

    private static async Task<(RecaudoService Sut, NewRichDbContext Db, Usuario Carmen, Usuario Rosa)> DosRecaudadoresAsync()
    {
        var (sut, db, carmen, _) = await PrepararVendedorConVentaAsync(new DateTime(2026, 9, 28, 20, 0, 0, DateTimeKind.Utc));
        await sut.RegistrarPagoAsync(carmen.UsuarioId, new RegistrarPagoRecaudoRequest
        {
            VendedorId = db.UsuariosGrupos.Single().UsuarioId,
            Valor = 60m,
            ClaveIdempotencia = "pago-tablero"
        }, CancellationToken.None);
        var sur = await AgregarGrupo(db, "Sur");
        var rosa = await AgregarUsuario(db, "Rosa Recaudo", RolUsuario.Recaudador);
        var beto = await AgregarUsuario(db, "Beto Sur", RolUsuario.Vendedor);
        db.UsuariosGrupos.Add(new UsuarioGrupo { UsuarioId = beto.UsuarioId, GrupoId = sur.GrupoId });
        db.Ventas.Add(new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = beto.UsuarioId,
            FechaVenta = new DateTime(2026, 9, 28, 9, 0, 0, DateTimeKind.Unspecified),
            Total = 2000m,
            TipoApuesta = TipoApuesta.INDIVIDUAL,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        await db.SaveChangesAsync();
        await sut.AsignarGrupoAsync(new AsignarGrupoRecaudoRequest
        {
            RecaudadorId = rosa.UsuarioId,
            GrupoId = sur.GrupoId,
            Porcentaje = 10
        }, rosa.UsuarioId, CancellationToken.None);
        return (sut, db, carmen, rosa);
    }

    private static async Task<(RecaudoService Sut, NewRichDbContext Db, Usuario Recaudador, Usuario Vendedor)> DeudaDelDiaAnteriorCobradaHoyAsync()
    {
        var (sut, db, recaudador, vendedor) = await PrepararVendedorConVentaAsync(
            new DateTime(2026, 9, 28, 20, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 27, 12, 0, 0, DateTimeKind.Unspecified));
        db.ObligacionesRecaudo.Add(new ObligacionRecaudo
        {
            ObligacionId = Guid.NewGuid(),
            VendedorId = vendedor.UsuarioId,
            RecaudadorId = recaudador.UsuarioId,
            Fecha = new DateTime(2026, 9, 27),
            TotalVendido = 1000m,
            Porcentaje = 10,
            ValorGenerado = 100m,
            FechaGeneracion = new DateTime(2026, 9, 27, 12, 0, 0)
        });
        await db.SaveChangesAsync();
        await sut.RegistrarPagoAsync(recaudador.UsuarioId, new RegistrarPagoRecaudoRequest
        {
            VendedorId = vendedor.UsuarioId,
            Valor = 60m,
            ClaveIdempotencia = "pago-deuda-anterior"
        }, CancellationToken.None);
        return (sut, db, recaudador, vendedor);
    }

    [Fact]
    public async Task La_linea_de_tiempo_llega_a_lo_sumo_a_31_dias_terminando_en_hasta()
    {
        var (sut, _, recaudador, _) = await PrepararVendedorConVentaAsync(new DateTime(2026, 9, 28, 20, 0, 0, DateTimeKind.Utc));

        var linea = await sut.LineaDeTiempoAsync(recaudador.UsuarioId, new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 28), CancellationToken.None);

        linea.Data!.Should().HaveCount(RecaudoService.DiasMaximosLineaDeTiempo);
        linea.Data.First().Fecha.Should().Be(new DateOnly(2026, 8, 29));
        linea.Data.Last().Fecha.Should().Be(new DateOnly(2026, 9, 28));
    }

    [Fact]
    public async Task El_detalle_agrupa_aparte_a_los_vendedores_sin_grupo()
    {
        var (sut, db, recaudador, _) = await PrepararVendedorConVentaAsync(new DateTime(2026, 9, 28, 20, 0, 0, DateTimeKind.Utc));
        var suelto = await AgregarUsuario(db, "Beto Suelto", RolUsuario.Vendedor);
        db.Ventas.Add(new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = suelto.UsuarioId,
            FechaVenta = new DateTime(2026, 9, 28, 11, 0, 0, DateTimeKind.Unspecified),
            Total = 2000m,
            TipoApuesta = TipoApuesta.INDIVIDUAL,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        await db.SaveChangesAsync();
        await sut.AsignarVendedorAsync(new AsignarVendedorRecaudoRequest
        {
            RecaudadorId = recaudador.UsuarioId,
            VendedorId = suelto.UsuarioId,
            Porcentaje = 20
        }, recaudador.UsuarioId, CancellationToken.None);
        var hoy = new DateOnly(2026, 9, 28);

        var detalle = await sut.DetalleAsync(recaudador.UsuarioId, hoy, hoy, CancellationToken.None);

        detalle.Data!.Grupos.Select(g => g.Nombre).Should().Equal("Centro", "Sin grupo");
        var sinGrupo = detalle.Data.Grupos.Single(g => g.Nombre == "Sin grupo");
        sinGrupo.Porcentaje.Should().BeNull();
        sinGrupo.Vendedores.Should().Be(1);
        sinGrupo.TotalPorRecaudar.Should().Be(400m);
        detalle.Data.Vendedores.Single(v => v.VendedorId == suelto.UsuarioId).UltimoPago.Should().BeNull();
    }

    [Theory]
    [InlineData(150, 100, 90, 50)]
    [InlineData(100, 100, 40, 0)]
    [InlineData(100, 0, 100, 100)]
    [InlineData(150, 100, 30, 30)]
    public void Pendiente_del_dia_es_lo_generado_despues_del_ultimo_cobro(
        decimal generadoHoy, decimal generadoAlCobrar, decimal totalPendiente, decimal esperado)
    {
        CalculoRecaudo.PendienteDelDia(generadoHoy, generadoAlCobrar, totalPendiente).Should().Be(esperado);
    }

    [Fact]
    public async Task Historial_y_metricas_responden_sin_movimientos()
    {
        var (sut, _) = Crear();
        var hoy = new DateOnly(2026, 9, 28);

        var historial = await sut.HistorialAsync(new FiltroHistorialRecaudo { Desde = hoy, Hasta = hoy }, CancellationToken.None);
        var metricas = await sut.MetricasAsync(hoy, hoy, CancellationToken.None);

        historial.IsSuccess.Should().BeTrue();
        historial.Data.Should().BeEmpty();
        metricas.IsSuccess.Should().BeTrue();
        metricas.Data!.TotalPorRecaudar.Should().Be(0m);
        metricas.Data.TotalRecaudado.Should().Be(0m);
    }

    [Fact]
    public async Task Consultar_grupos_y_vendedores_devuelve_la_configuracion()
    {
        var (sut, db) = Crear();
        await AgregarGrupo(db, "Norte");
        var hoy = new DateOnly(2026, 9, 28);

        var grupos = await sut.GruposAsync(hoy, hoy, CancellationToken.None);
        var vendedores = await sut.VendedoresAsync(hoy, hoy, CancellationToken.None);

        grupos.IsSuccess.Should().BeTrue();
        grupos.Data.Should().ContainSingle(g => g.Nombre == "Norte" && g.SinConfigurar);
        vendedores.IsSuccess.Should().BeTrue();
        vendedores.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task Integrantes_del_grupo_incluyen_a_todos_los_vendedores_con_su_pendiente()
    {
        var (sut, db) = Crear();
        var grupo = await AgregarGrupo(db, "Centro");
        var recaudador = await AgregarUsuario(db, "Carmen Recaudo", RolUsuario.Recaudador);
        var ana = await AgregarUsuario(db, "Ana Perez", RolUsuario.Vendedor);
        var beto = await AgregarUsuario(db, "Beto Diaz", RolUsuario.Vendedor);
        db.UsuariosGrupos.AddRange(
            new UsuarioGrupo { UsuarioId = ana.UsuarioId, GrupoId = grupo.GrupoId },
            new UsuarioGrupo { UsuarioId = beto.UsuarioId, GrupoId = grupo.GrupoId });
        db.Ventas.Add(new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = ana.UsuarioId,
            FechaVenta = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc),
            Total = 1005m,
            TipoApuesta = TipoApuesta.INDIVIDUAL,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        await db.SaveChangesAsync();
        await sut.AsignarGrupoAsync(new AsignarGrupoRecaudoRequest
        {
            RecaudadorId = recaudador.UsuarioId,
            GrupoId = grupo.GrupoId,
            Porcentaje = 10
        }, recaudador.UsuarioId, CancellationToken.None);
        var hoy = new DateOnly(2026, 9, 28);

        var integrantes = await sut.IntegrantesGrupoAsync(grupo.GrupoId, hoy, hoy, CancellationToken.None);

        integrantes.IsSuccess.Should().BeTrue();
        integrantes.Data!.Nombre.Should().Be("Centro");
        integrantes.Data.RecaudadorNombre.Should().Be("Carmen Recaudo");
        integrantes.Data.Porcentaje.Should().Be(10);
        integrantes.Data.Integrantes.Should().HaveCount(2);
        integrantes.Data.Integrantes.Should().Contain(i => i.VendedorId == ana.UsuarioId && i.ValorACobrar == 101m && i.TotalVendido == 1005m);
        integrantes.Data.Integrantes.Should().Contain(i => i.VendedorId == beto.UsuarioId && i.TotalVendido == 0m);
        integrantes.Data.TotalPendiente.Should().Be(integrantes.Data.Integrantes.Sum(i => i.TotalPendiente));
        integrantes.Data.TotalPendiente.Should().Be(101m);
    }

    [Fact]
    public async Task Integrantes_muestran_las_ventas_con_el_porcentaje_guardado_aunque_el_grupo_no_tenga_recaudador()
    {
        var (sut, db) = Crear();
        var (grupo, ana) = await GrupoConVentaDeAnaAsync(db, 29500m);
        db.PorcentajesGrupoRecaudo.Add(new PorcentajeGrupoRecaudo { GrupoId = grupo.GrupoId, Porcentaje = 5 });
        await db.SaveChangesAsync();
        var hoy = new DateOnly(2026, 9, 28);

        var integrantes = await sut.IntegrantesGrupoAsync(grupo.GrupoId, hoy, hoy, CancellationToken.None);

        integrantes.Data!.RecaudadorNombre.Should().BeEmpty();
        integrantes.Data.Porcentaje.Should().Be(5);
        integrantes.Data.Integrantes.Should().Contain(i => i.VendedorId == ana.UsuarioId
            && i.TotalVendido == 29500m && i.ValorACobrar == 1475m && i.TotalPendiente == 1475m && i.Porcentaje == 5);
        integrantes.Data.TotalPorRecaudar.Should().Be(1475m);
        integrantes.Data.TotalPendiente.Should().Be(1475m);
    }

    [Fact]
    public async Task Integrantes_muestran_las_ventas_aunque_el_recaudador_asignado_ya_no_exista()
    {
        var (sut, db) = Crear();
        var (grupo, ana) = await GrupoConVentaDeAnaAsync(db, 29500m);
        db.AsignacionesGrupoRecaudo.Add(new AsignacionGrupoRecaudo
        {
            AsignacionId = Guid.NewGuid(),
            RecaudadorId = Guid.NewGuid(),
            GrupoId = grupo.GrupoId,
            Porcentaje = 5,
            Estado = "Activa"
        });
        await db.SaveChangesAsync();
        var hoy = new DateOnly(2026, 9, 28);

        var integrantes = await sut.IntegrantesGrupoAsync(grupo.GrupoId, hoy, hoy, CancellationToken.None);

        integrantes.Data!.RecaudadorNombre.Should().BeEmpty();
        integrantes.Data.Integrantes.Should().Contain(i => i.VendedorId == ana.UsuarioId
            && i.TotalVendido == 29500m && i.ValorACobrar == 1475m && i.TotalPendiente == 1475m);
        integrantes.Data.TotalPorRecaudar.Should().Be(1475m);
        integrantes.Data.TotalPendiente.Should().Be(1475m);
    }

    [Fact]
    public async Task Grupos_muestran_lo_por_recaudar_con_el_porcentaje_guardado_aunque_no_tengan_recaudador()
    {
        var (sut, db) = Crear();
        var (grupo, _) = await GrupoConVentaDeAnaAsync(db, 29500m);
        db.PorcentajesGrupoRecaudo.Add(new PorcentajeGrupoRecaudo { GrupoId = grupo.GrupoId, Porcentaje = 5 });
        await db.SaveChangesAsync();
        var hoy = new DateOnly(2026, 9, 28);

        var grupos = await sut.GruposAsync(hoy, hoy, CancellationToken.None);

        grupos.Data.Should().Contain(g => g.GrupoId == grupo.GrupoId
            && g.TotalPorRecaudar == 1475m && g.RecaudadorNombre == string.Empty);
    }

    [Fact]
    public async Task Grupos_muestran_lo_por_recaudar_aunque_el_recaudador_asignado_ya_no_exista()
    {
        var (sut, db) = Crear();
        var (grupo, _) = await GrupoConVentaDeAnaAsync(db, 29500m);
        db.AsignacionesGrupoRecaudo.Add(new AsignacionGrupoRecaudo
        {
            AsignacionId = Guid.NewGuid(),
            RecaudadorId = Guid.NewGuid(),
            GrupoId = grupo.GrupoId,
            Porcentaje = 5,
            Estado = "Activa"
        });
        await db.SaveChangesAsync();
        var hoy = new DateOnly(2026, 9, 28);

        var grupos = await sut.GruposAsync(hoy, hoy, CancellationToken.None);

        grupos.Data.Should().Contain(g => g.GrupoId == grupo.GrupoId
            && g.TotalPorRecaudar == 1475m && g.RecaudadorNombre == string.Empty);
    }

    private static async Task<(Grupo Grupo, Usuario Ana)> GrupoConVentaDeAnaAsync(NewRichDbContext db, decimal total)
    {
        var grupo = await AgregarGrupo(db, "Centro");
        var ana = await AgregarUsuario(db, "Ana Perez", RolUsuario.Vendedor);
        db.UsuariosGrupos.Add(new UsuarioGrupo { UsuarioId = ana.UsuarioId, GrupoId = grupo.GrupoId });
        db.Ventas.Add(new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = ana.UsuarioId,
            FechaVenta = new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc),
            Total = total,
            TipoApuesta = TipoApuesta.INDIVIDUAL,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        await db.SaveChangesAsync();
        return (grupo, ana);
    }

    [Fact]
    public async Task Actualizar_porcentajes_cambia_todos_los_grupos_en_un_solo_movimiento()
    {
        var (sut, db) = Crear();
        var norte = await AgregarGrupo(db, "Norte");
        var sur = await AgregarGrupo(db, "Sur");
        var recaudador = await AgregarUsuario(db, "Carmen Recaudo", RolUsuario.Recaudador);
        foreach (var grupo in new[] { norte, sur })
        {
            await sut.AsignarGrupoAsync(new AsignarGrupoRecaudoRequest
            {
                RecaudadorId = recaudador.UsuarioId,
                GrupoId = grupo.GrupoId,
                Porcentaje = 10
            }, recaudador.UsuarioId, CancellationToken.None);
        }

        var resultado = await sut.ActualizarPorcentajesGruposAsync(new ActualizarPorcentajesGruposRecaudoRequest
        {
            Grupos =
            [
                new PorcentajeGrupoRecaudoRequest { GrupoId = norte.GrupoId, Porcentaje = 25 },
                new PorcentajeGrupoRecaudoRequest { GrupoId = sur.GrupoId, Porcentaje = 40 }
            ]
        }, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        var hoy = new DateOnly(2026, 9, 28);
        var config = await sut.ConfiguracionAsync(hoy, hoy, CancellationToken.None);
        config.Data!.Grupos.Should().Contain(g => g.Nombre == "Norte" && g.Porcentaje == 25);
        config.Data.Grupos.Should().Contain(g => g.Nombre == "Sur" && g.Porcentaje == 40);
    }

    [Fact]
    public async Task Actualizar_porcentajes_con_un_valor_invalido_no_guarda_ninguno()
    {
        var (sut, db) = Crear();
        var norte = await AgregarGrupo(db, "Norte");
        var sur = await AgregarGrupo(db, "Sur");
        var recaudador = await AgregarUsuario(db, "Carmen Recaudo", RolUsuario.Recaudador);
        foreach (var grupo in new[] { norte, sur })
        {
            await sut.AsignarGrupoAsync(new AsignarGrupoRecaudoRequest
            {
                RecaudadorId = recaudador.UsuarioId,
                GrupoId = grupo.GrupoId,
                Porcentaje = 10
            }, recaudador.UsuarioId, CancellationToken.None);
        }

        var resultado = await sut.ActualizarPorcentajesGruposAsync(new ActualizarPorcentajesGruposRecaudoRequest
        {
            Grupos =
            [
                new PorcentajeGrupoRecaudoRequest { GrupoId = norte.GrupoId, Porcentaje = 30 },
                new PorcentajeGrupoRecaudoRequest { GrupoId = sur.GrupoId, Porcentaje = 101 }
            ]
        }, CancellationToken.None);

        resultado.IsSuccess.Should().BeFalse();
        var hoy = new DateOnly(2026, 9, 28);
        var config = await sut.ConfiguracionAsync(hoy, hoy, CancellationToken.None);
        config.Data!.Grupos.Should().OnlyContain(g => g.Porcentaje == 10);
    }

    [Fact]
    public async Task Actualizar_porcentajes_guarda_el_de_un_grupo_sin_recaudador()
    {
        var (sut, db) = Crear();
        var norte = await AgregarGrupo(db, "Norte");

        var resultado = await sut.ActualizarPorcentajesGruposAsync(new ActualizarPorcentajesGruposRecaudoRequest
        {
            Grupos = [new PorcentajeGrupoRecaudoRequest { GrupoId = norte.GrupoId, Porcentaje = 20 }]
        }, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        var hoy = new DateOnly(2026, 9, 28);
        var config = await sut.ConfiguracionAsync(hoy, hoy, CancellationToken.None);
        config.Data!.Grupos.Should().ContainSingle(g => g.Nombre == "Norte" && g.Porcentaje == 20 && g.SinConfigurar);
    }

    [Fact]
    public async Task Asignar_grupo_sin_porcentaje_usa_el_guardado_en_el_grupo()
    {
        var (sut, db) = Crear();
        var norte = await AgregarGrupo(db, "Norte");
        var recaudador = await AgregarUsuario(db, "Carmen Recaudo", RolUsuario.Recaudador);
        await sut.ActualizarPorcentajesGruposAsync(new ActualizarPorcentajesGruposRecaudoRequest
        {
            Grupos = [new PorcentajeGrupoRecaudoRequest { GrupoId = norte.GrupoId, Porcentaje = 15 }]
        }, CancellationToken.None);

        var asignado = await sut.AsignarGrupoAsync(new AsignarGrupoRecaudoRequest
        {
            RecaudadorId = recaudador.UsuarioId,
            GrupoId = norte.GrupoId,
            Porcentaje = 0
        }, recaudador.UsuarioId, CancellationToken.None);

        asignado.IsSuccess.Should().BeTrue();
        var hoy = new DateOnly(2026, 9, 28);
        var config = await sut.ConfiguracionAsync(hoy, hoy, CancellationToken.None);
        config.Data!.Grupos.Should().ContainSingle(g => g.Nombre == "Norte" && g.Porcentaje == 15 && !g.SinConfigurar);
    }

    [Fact]
    public async Task Actualizar_porcentajes_en_cero_quita_el_porcentaje_de_un_grupo_sin_recaudador()
    {
        var (sut, db) = Crear();
        var norte = await AgregarGrupo(db, "Norte");
        await sut.ActualizarPorcentajesGruposAsync(new ActualizarPorcentajesGruposRecaudoRequest
        {
            Grupos = [new PorcentajeGrupoRecaudoRequest { GrupoId = norte.GrupoId, Porcentaje = 20 }]
        }, CancellationToken.None);

        var resultado = await sut.ActualizarPorcentajesGruposAsync(new ActualizarPorcentajesGruposRecaudoRequest
        {
            Grupos = [new PorcentajeGrupoRecaudoRequest { GrupoId = norte.GrupoId, Porcentaje = 0 }]
        }, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        var hoy = new DateOnly(2026, 9, 28);
        var config = await sut.ConfiguracionAsync(hoy, hoy, CancellationToken.None);
        config.Data!.Grupos.Should().ContainSingle(g => g.Nombre == "Norte" && g.Porcentaje == 0);
    }

    [Fact]
    public async Task Actualizar_porcentajes_en_cero_no_se_permite_en_un_grupo_con_recaudador()
    {
        var (sut, db) = Crear();
        var norte = await AgregarGrupo(db, "Norte");
        var recaudador = await AgregarUsuario(db, "Carmen Recaudo", RolUsuario.Recaudador);
        await sut.AsignarGrupoAsync(new AsignarGrupoRecaudoRequest
        {
            RecaudadorId = recaudador.UsuarioId,
            GrupoId = norte.GrupoId,
            Porcentaje = 10
        }, recaudador.UsuarioId, CancellationToken.None);

        var resultado = await sut.ActualizarPorcentajesGruposAsync(new ActualizarPorcentajesGruposRecaudoRequest
        {
            Grupos = [new PorcentajeGrupoRecaudoRequest { GrupoId = norte.GrupoId, Porcentaje = 0 }]
        }, CancellationToken.None);

        resultado.IsSuccess.Should().BeFalse();
        resultado.Message.Should().Contain("Norte");
        var hoy = new DateOnly(2026, 9, 28);
        var config = await sut.ConfiguracionAsync(hoy, hoy, CancellationToken.None);
        config.Data!.Grupos.Should().ContainSingle(g => g.Nombre == "Norte" && g.Porcentaje == 10);
    }

    [Fact]
    public async Task Actualizar_porcentajes_rechaza_un_grupo_que_no_existe()
    {
        var (sut, _) = Crear();

        var resultado = await sut.ActualizarPorcentajesGruposAsync(new ActualizarPorcentajesGruposRecaudoRequest
        {
            Grupos = [new PorcentajeGrupoRecaudoRequest { GrupoId = Guid.NewGuid(), Porcentaje = 20 }]
        }, CancellationToken.None);

        resultado.IsSuccess.Should().BeFalse();
    }

    private static (RecaudoService Sut, NewRichDbContext Db) Crear(DateTime? utcNow = null)
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var reloj = new RelojFijo(utcNow ?? new DateTime(2026, 9, 28, 15, 0, 0, DateTimeKind.Utc));
        return (new RecaudoService(db, reloj), db);
    }

    private static async Task<(RecaudoService Sut, NewRichDbContext Db, Usuario Recaudador, Usuario Vendedor)> PrepararVendedorConVentaAsync(
        DateTime utcNow,
        DateTime? fechaVentaUtc = null)
    {
        var (sut, db) = Crear(utcNow);
        var grupo = await AgregarGrupo(db, "Centro");
        var recaudador = await AgregarUsuario(db, "Carmen Recaudo", RolUsuario.Recaudador);
        var vendedor = await AgregarUsuario(db, "Ana Vende", RolUsuario.Vendedor);
        db.UsuariosGrupos.Add(new UsuarioGrupo { UsuarioId = vendedor.UsuarioId, GrupoId = grupo.GrupoId });
        db.Ventas.Add(new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = vendedor.UsuarioId,
            FechaVenta = fechaVentaUtc ?? new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc),
            Total = 1000m,
            TipoApuesta = TipoApuesta.INDIVIDUAL,
            IdempotencyKey = Guid.NewGuid().ToString("N")
        });
        await db.SaveChangesAsync();
        await sut.AsignarGrupoAsync(new AsignarGrupoRecaudoRequest
        {
            RecaudadorId = recaudador.UsuarioId,
            GrupoId = grupo.GrupoId,
            Porcentaje = 10
        }, recaudador.UsuarioId, CancellationToken.None);
        return (sut, db, recaudador, vendedor);
    }

    private static async Task<Grupo> AgregarGrupo(NewRichDbContext db, string nombre)
    {
        var grupo = new Grupo { GrupoId = Guid.NewGuid(), Nombre = nombre, FechaCreacion = DateTime.UtcNow };
        db.Grupos.Add(grupo);
        await db.SaveChangesAsync();
        return grupo;
    }

    private static async Task<Usuario> AgregarUsuario(NewRichDbContext db, string nombre, RolUsuario rol)
    {
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = nombre,
            NombreUsuario = nombre.Replace(' ', '.').ToLowerInvariant(),
            PasswordHash = "hash",
            PasswordSalt = "salt",
            Rol = rol,
            FechaCreacion = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    private sealed class RelojFijo(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
        public DateTime LocalNow => ZonaHorariaColombia.ALocal(UtcNow);
    }
}
