using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Application.Services;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
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

    private static (RecaudoService Sut, NewRichDbContext Db) Crear()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        return (new RecaudoService(db, new RelojFijo(new DateTime(2026, 9, 28, 15, 0, 0, DateTimeKind.Utc))), db);
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
        public DateTime LocalNow => UtcNow;
    }
}
