using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Services;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class KpiServiceTests
{
    private static readonly DateTime Corte = new(2026, 8, 30, 19, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ConsultarAsync_sin_ventas_deja_ingresos_en_cero()
    {
        var (sut, _) = CreateSut();

        var result = await sut.ConsultarAsync(new KpiRequest
        {
            FechaInicial = new DateTime(2026, 8, 1),
            FechaFinal = new DateTime(2026, 8, 30)
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Ingresos.Should().Be(0);
        result.Data.VentasConfirmadas.Should().Be(0);
        result.Data.IngresosPorVendedor.Should().BeEmpty();
    }

    [Fact]
    public async Task ConsultarAsync_filtra_ventas_por_rango_de_fechas()
    {
        var (sut, db) = CreateSut();
        var vendedor = await AgregarUsuarioAsync(db, "Camila Rojas", RolUsuario.Vendedor);
        await AgregarVentaAsync(db, vendedor, new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc), 1000);
        await AgregarVentaAsync(db, vendedor, new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc), 9000);

        var result = await sut.ConsultarAsync(new KpiRequest
        {
            FechaInicial = new DateTime(2026, 8, 1),
            FechaFinal = new DateTime(2026, 8, 30)
        }, CancellationToken.None);

        result.Data!.Ingresos.Should().Be(1000);
        result.Data.VentasConfirmadas.Should().Be(1);
    }

    [Fact]
    public async Task ConsultarAsync_filtra_por_grupo_y_excluye_otros_vendedores()
    {
        var (sut, db) = CreateSut();
        var norte = await AgregarGrupoAsync(db, "Grupo Norte");
        var camila = await AgregarUsuarioAsync(db, "Camila Rojas", RolUsuario.Vendedor);
        var jorge = await AgregarUsuarioAsync(db, "Jorge Mena", RolUsuario.Vendedor);
        db.UsuariosGrupos.Add(new UsuarioGrupo { UsuarioId = camila.UsuarioId, GrupoId = norte.GrupoId });
        await db.SaveChangesAsync();
        await AgregarVentaAsync(db, camila, new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc), 5000);
        await AgregarVentaAsync(db, jorge, new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc), 2000);

        var result = await sut.ConsultarAsync(new KpiRequest
        {
            GrupoId = norte.GrupoId,
            FechaInicial = new DateTime(2026, 8, 1),
            FechaFinal = new DateTime(2026, 8, 30)
        }, CancellationToken.None);

        result.Data!.Ingresos.Should().Be(5000);
        result.Data.IngresosPorVendedor.Should().ContainSingle(v => v.Vendedor == "Camila Rojas" && v.Total == 5000);
        result.Data.Vendedores.Should().Be(1);
    }

    [Fact]
    public async Task ConsultarAsync_filtra_por_vendedor()
    {
        var (sut, db) = CreateSut();
        var camila = await AgregarUsuarioAsync(db, "Camila Rojas", RolUsuario.Vendedor);
        var jorge = await AgregarUsuarioAsync(db, "Jorge Mena", RolUsuario.Vendedor);
        await AgregarVentaAsync(db, camila, new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc), 3000);
        await AgregarVentaAsync(db, jorge, new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc), 1000);

        var result = await sut.ConsultarAsync(new KpiRequest
        {
            VendedorId = camila.UsuarioId,
            FechaInicial = new DateTime(2026, 8, 1),
            FechaFinal = new DateTime(2026, 8, 30)
        }, CancellationToken.None);

        result.Data!.Ingresos.Should().Be(3000);
        result.Data.VentasConfirmadas.Should().Be(1);
    }

    [Fact]
    public async Task ConsultarAsync_compara_con_el_periodo_anterior()
    {
        var (sut, db) = CreateSut();
        var vendedor = await AgregarUsuarioAsync(db, "Camila Rojas", RolUsuario.Vendedor);
        await AgregarVentaAsync(db, vendedor, new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc), 2000);
        await AgregarVentaAsync(db, vendedor, new DateTime(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc), 1000);

        var result = await sut.ConsultarAsync(new KpiRequest
        {
            FechaInicial = new DateTime(2026, 8, 16),
            FechaFinal = new DateTime(2026, 8, 30),
            CompararAnterior = true
        }, CancellationToken.None);

        result.Data!.Ingresos.Should().Be(2000);
        result.Data.VariacionIngresos.Should().Be(100);
    }

    [Fact]
    public async Task ConsultarAsync_incluye_numeros_ganadores_del_rango()
    {
        var (sut, db) = CreateSut();
        var loteria = new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Medellin",
            Estado = EstadoGeneral.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        db.Loterias.Add(loteria);
        db.NumerosGanadores.Add(new NumeroGanador
        {
            NumeroGanadorId = Guid.NewGuid(),
            LoteriaId = loteria.LoteriaId,
            Numero = "1234",
            FechaJuego = new DateTime(2026, 8, 20),
            FechaRegistro = DateTime.UtcNow,
            Loteria = loteria
        });
        await db.SaveChangesAsync();

        var result = await sut.ConsultarAsync(new KpiRequest
        {
            FechaInicial = new DateTime(2026, 8, 1),
            FechaFinal = new DateTime(2026, 8, 30)
        }, CancellationToken.None);

        result.Data!.NumerosGanadores.Should().Be(1);
        result.Data.Resultados.Should().ContainSingle(r => r.Numero == "1234" && r.Loteria == "Medellin");
    }

    private static (KpiService Sut, NewRichDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        return (new KpiService(db, new FixedClock(Corte)), db);
    }

    private static async Task<Usuario> AgregarUsuarioAsync(NewRichDbContext db, string nombre, RolUsuario rol)
    {
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = nombre,
            NombreUsuario = Guid.NewGuid().ToString("N")[..8],
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = rol,
            Estado = EstadoUsuario.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario;
    }

    private static async Task<Grupo> AgregarGrupoAsync(NewRichDbContext db, string nombre)
    {
        var grupo = new Grupo
        {
            GrupoId = Guid.NewGuid(),
            Nombre = nombre,
            FechaCreacion = DateTime.UtcNow
        };
        db.Grupos.Add(grupo);
        await db.SaveChangesAsync();
        return grupo;
    }

    private static async Task AgregarVentaAsync(NewRichDbContext db, Usuario vendedor, DateTime fecha, decimal total)
    {
        db.Ventas.Add(new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = vendedor.UsuarioId,
            Usuario = vendedor,
            FechaVenta = fecha,
            Total = total,
            TipoApuesta = TipoApuesta.COMBINADO
        });
        await db.SaveChangesAsync();
    }

    private sealed class FixedClock : NewRich.Application.Abstractions.IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }
}
