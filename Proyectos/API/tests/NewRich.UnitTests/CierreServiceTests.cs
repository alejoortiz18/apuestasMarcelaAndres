using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Cierre;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class CierreServiceTests
{
    [Fact]
    public async Task El_primer_cierre_toma_lo_abierto_y_deja_fuera_lo_posterior()
    {
        var corte = new DateTime(2026, 11, 6, 16, 0, 0);
        var (sut, db, admin, _) = Crear(corte);
        var dentro = Venta(admin.UsuarioId, corte, 1000m);
        var fuera = Venta(admin.UsuarioId, corte.AddSeconds(1), 50m);
        var recaudo = new PagoRegistradoRecaudo
        {
            PagoId = Guid.NewGuid(),
            ObligacionId = Guid.NewGuid(),
            RecaudadorId = admin.UsuarioId,
            VendedorId = admin.UsuarioId,
            Valor = 1000m,
            FechaHora = corte.AddMinutes(-1),
            ClaveIdempotencia = "pago"
        };
        var tipo = new TipoGasto { TipoGastoId = Guid.NewGuid(), Nombre = "Transporte", Activo = true, FechaCreacion = corte, UsuarioCreacionId = admin.UsuarioId };
        var gasto = new Gasto
        {
            GastoId = Guid.NewGuid(),
            TipoGastoId = tipo.TipoGastoId,
            Tipo = tipo,
            FechaGasto = new DateOnly(2026, 11, 6),
            Valor = 80m,
            Estado = EstadoGasto.Activo,
            UsuarioRegistroId = admin.UsuarioId,
            ClaveIdempotencia = "gasto",
            FechaCreacion = corte
        };
        db.Ventas.AddRange(dentro, fuera);
        db.TiposGasto.Add(tipo);
        db.Gastos.Add(gasto);
        db.PagosRecaudo.Add(recaudo);
        await db.SaveChangesAsync();

        var previa = await sut.PreviaAsync(CancellationToken.None);
        previa.Data!.HayCierreAnterior.Should().BeFalse();
        previa.Data.FechaInicio.Should().BeNull();
        previa.Data.Dias.Should().Be(1);
        previa.Data.TotalGanancia.Should().Be(920m);
        previa.Data.AperturaSiguiente.Should().Be(corte.AddSeconds(1));

        var cerrado = await sut.ConfirmarAsync(admin.UsuarioId, new ConfirmarCierreRequest { Password = "clave" }, CancellationToken.None);

        cerrado.IsSuccess.Should().BeTrue();
        cerrado.Data!.FechaInicio.Should().BeNull();
        cerrado.Data.TotalGanancia.Should().Be(920m);
        (await db.Ventas.SingleAsync(v => v.VentaId == dentro.VentaId)).CierreId.Should().Be(cerrado.Data.CierreId);
        (await db.Ventas.SingleAsync(v => v.VentaId == fuera.VentaId)).CierreId.Should().BeNull();
        (await db.Gastos.SingleAsync()).CierreId.Should().Be(cerrado.Data.CierreId);
        (await db.PagosRecaudo.SingleAsync()).CierreId.Should().Be(cerrado.Data.CierreId);
    }

    [Fact]
    public async Task El_cierre_marca_los_pagos_a_la_administracion()
    {
        var corte = new DateTime(2026, 11, 6, 16, 0, 0);
        var (sut, db, admin, _) = Crear(corte);
        var grupoId = Guid.NewGuid();
        db.Grupos.Add(new Grupo { GrupoId = grupoId, Nombre = "Norte", FechaCreacion = corte });
        var dentro = new PagoAdministracionRecaudoEntity
        {
            PagoId = Guid.NewGuid(),
            GrupoId = grupoId,
            RecaudadorId = admin.UsuarioId,
            Valor = 25_000m,
            FechaOperacion = DateOnly.FromDateTime(corte),
            FechaRegistroUtc = corte.ToUniversalTime()
        };
        var fuera = new PagoAdministracionRecaudoEntity
        {
            PagoId = Guid.NewGuid(),
            GrupoId = grupoId,
            RecaudadorId = admin.UsuarioId,
            Valor = 5_000m,
            FechaOperacion = DateOnly.FromDateTime(corte.AddDays(1)),
            FechaRegistroUtc = corte.AddDays(1).ToUniversalTime()
        };
        db.PagosAdministracionRecaudo.AddRange(dentro, fuera);
        await db.SaveChangesAsync();

        var cerrado = await sut.ConfirmarAsync(admin.UsuarioId, new ConfirmarCierreRequest { Password = "clave" }, CancellationToken.None);

        cerrado.IsSuccess.Should().BeTrue(cerrado.Message);
        cerrado.Data!.TotalRecaudos.Should().Be(25_000m);
        (await db.PagosAdministracionRecaudo.SingleAsync(p => p.PagoId == dentro.PagoId)).CierreId.Should().Be(cerrado.Data.CierreId);
        (await db.PagosAdministracionRecaudo.SingleAsync(p => p.PagoId == fuera.PagoId)).CierreId.Should().BeNull();

        var detalle = await sut.DetalleAsync(cerrado.Data.CierreId, ClavesCierre.Recaudos, 1, 10, "valor", "asc", CancellationToken.None);
        detalle.Data!.Categorias.Should().ContainSingle(c => c.Clave == ClavesCierre.Recaudos && c.Cantidad == 1 && c.Valor == 25_000m);
        detalle.Data.Movimientos.Should().ContainSingle()
            .Which.Detalle.Should().Be("Pago a la administración");
        detalle.Data.Movimientos[0].Valor.Should().Be(25_000m);
    }

    [Fact]
    public async Task Una_contrasena_incorrecta_no_crea_el_cierre()
    {
        var (sut, db, admin, _) = Crear(new DateTime(2026, 11, 6, 16, 0, 0));

        var resultado = await sut.ConfirmarAsync(admin.UsuarioId, new ConfirmarCierreRequest { Password = "no" }, CancellationToken.None);

        resultado.IsSuccess.Should().BeFalse();
        resultado.StatusCode.Should().Be(403);
        resultado.Message.Should().Be(AuthMessages.ContrasenaAccionIncorrecta);
        db.Cierres.Should().BeEmpty();
    }

    [Fact]
    public async Task El_segundo_cierre_empieza_un_segundo_despues_y_no_repite_movimientos()
    {
        var primero = new DateTime(2026, 10, 1, 14, 0, 0);
        var (sut, db, admin, reloj) = Crear(primero);
        db.Ventas.Add(Venta(admin.UsuarioId, primero.AddHours(-1), 200m));
        await db.SaveChangesAsync();
        await sut.ConfirmarAsync(admin.UsuarioId, new ConfirmarCierreRequest { Password = "clave" }, CancellationToken.None);

        reloj.Ahora = new DateTime(2026, 11, 6, 16, 0, 0);
        db.Ventas.Add(Venta(admin.UsuarioId, reloj.Ahora, 40m));
        await db.SaveChangesAsync();

        var cerrado = await sut.ConfirmarAsync(admin.UsuarioId, new ConfirmarCierreRequest { Password = "clave" }, CancellationToken.None);

        cerrado.Data!.FechaInicio.Should().Be(primero.AddSeconds(1));
        cerrado.Data.Dias.Should().Be(37);
        cerrado.Data.TotalVentas.Should().Be(40m);
        db.Cierres.Should().HaveCount(2);
        db.Ventas.Count(v => v.CierreId == cerrado.Data.CierreId).Should().Be(1);
    }

    [Fact]
    public async Task El_detalle_ordena_todos_los_movimientos_antes_de_paginar()
    {
        var corte = new DateTime(2026, 11, 6, 16, 0, 0);
        var (sut, db, admin, _) = Crear(corte);
        db.Ventas.AddRange(
            Venta(admin.UsuarioId, corte.AddMinutes(-2), 500m),
            Venta(admin.UsuarioId, corte.AddMinutes(-1), 100m));
        await db.SaveChangesAsync();
        var cerrado = await sut.ConfirmarAsync(admin.UsuarioId, new ConfirmarCierreRequest { Password = "clave" }, CancellationToken.None);

        var detalle = await sut.DetalleAsync(cerrado.Data!.CierreId, "ventas", 1, 10, "valor", "asc", CancellationToken.None);

        detalle.Data!.Movimientos.Select(m => m.Valor).Should().Equal(100m, 500m);
        detalle.Data.Comparacion.Should().ContainSingle(c => c.Detalle == "Venta del PDA" && c.Cantidad == 2 && c.Valor == 600m);
    }

    [Fact]
    public async Task Un_vendedor_no_puede_cerrar()
    {
        var (sut, db, _, _) = Crear(new DateTime(2026, 11, 6, 16, 0, 0));
        var vendedor = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "Ana",
            NombreUsuario = "ana",
            PasswordHash = "hash",
            PasswordSalt = "salt",
            Rol = RolUsuario.Vendedor,
            FechaCreacion = DateTime.UtcNow
        };
        db.Usuarios.Add(vendedor);
        await db.SaveChangesAsync();

        var resultado = await sut.ConfirmarAsync(vendedor.UsuarioId, new ConfirmarCierreRequest { Password = "clave" }, CancellationToken.None);

        resultado.StatusCode.Should().Be(403);
        db.Cierres.Should().BeEmpty();
    }

    private static (CierreService Sut, NewRichDbContext Db, Usuario Admin, Reloj Reloj) Crear(DateTime ahora)
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new NewRichDbContext(options);
        var reloj = new Reloj { Ahora = ahora };
        var admin = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "Admin",
            NombreUsuario = "admin",
            PasswordHash = "hash",
            PasswordSalt = "salt",
            Rol = RolUsuario.Administrador,
            FechaCreacion = ahora
        };
        db.Usuarios.Add(admin);
        db.SaveChanges();
        return (new CierreService(db, reloj, new ClaveFija()), db, admin, reloj);
    }

    private static Venta Venta(Guid usuarioId, DateTime fecha, decimal total) => new()
    {
        VentaId = Guid.NewGuid(),
        UsuarioId = usuarioId,
        FechaVenta = fecha,
        Total = total,
        TipoApuesta = TipoApuesta.INDIVIDUAL,
        IdempotencyKey = Guid.NewGuid().ToString("N")
    };

    private sealed class Reloj : IClock
    {
        public DateTime Ahora { get; set; }
        public DateTime UtcNow => Ahora;
        public DateTime LocalNow => Ahora;
    }

    private sealed class ClaveFija : IPasswordHasher
    {
        public (string Hash, string Salt) Hash(string password) => (password, "salt");
        public bool Verify(string password, string hash, string salt) => password == "clave";
    }
}
