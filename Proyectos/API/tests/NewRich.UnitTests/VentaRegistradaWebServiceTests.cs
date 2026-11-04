using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.VentasRegistradasWeb;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class VentaRegistradaWebServiceTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 22);
    private const string Clave = "secreta";

    [Fact]
    public async Task Registrar_guarda_la_venta_de_hoy_del_vendedor()
    {
        await using var db = Nueva();
        var vendedor = await Vendedor(db, "Ana");
        var sut = new VentaRegistradaWebService(db, new HasherFalso());

        var guardada = await sut.RegistrarAsync(vendedor, new RegistrarVentaWebRequest(Hoy, 150_000m, Clave), Hoy, CancellationToken.None);

        guardada.IsSuccess.Should().BeTrue(guardada.Message);
        guardada.StatusCode.Should().Be(201);
        guardada.Data!.FechaVenta.Should().Be(Hoy);
        guardada.Data.Valor.Should().Be(150_000m);
        var fila = await db.VentasRegistradasWeb.SingleAsync();
        fila.VendedorId.Should().Be(vendedor);
        fila.FechaVenta.Should().Be(Hoy);
        fila.Valor.Should().Be(150_000m);
    }

    [Fact]
    public async Task Registrar_rechaza_contrasena_incorrecta_o_vacia_sin_guardar()
    {
        await using var db = Nueva();
        var vendedor = await Vendedor(db, "Ana");
        var sut = new VentaRegistradaWebService(db, new HasherFalso());

        var incorrecta = await sut.RegistrarAsync(vendedor, new RegistrarVentaWebRequest(Hoy, 10_000m, "otra"), Hoy, CancellationToken.None);
        var vacia = await sut.RegistrarAsync(vendedor, new RegistrarVentaWebRequest(Hoy, 10_000m, " "), Hoy, CancellationToken.None);

        incorrecta.Message.Should().Be(VentaRegistradaWebMessages.ContrasenaIncorrecta);
        incorrecta.StatusCode.Should().Be(400);
        vacia.Message.Should().Be(VentaRegistradaWebMessages.ContrasenaRequerida);
        (await db.VentasRegistradasWeb.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Registrar_solo_admite_la_fecha_de_hoy_y_valor_entero_positivo()
    {
        await using var db = Nueva();
        var vendedor = await Vendedor(db, "Ana");
        var sut = new VentaRegistradaWebService(db, new HasherFalso());

        (await sut.RegistrarAsync(vendedor, new RegistrarVentaWebRequest(Hoy.AddDays(-1), 10_000m, Clave), Hoy, CancellationToken.None))
            .Message.Should().Be(VentaRegistradaWebMessages.SoloHoy);
        (await sut.RegistrarAsync(vendedor, new RegistrarVentaWebRequest(Hoy.AddDays(1), 10_000m, Clave), Hoy, CancellationToken.None))
            .Message.Should().Be(VentaRegistradaWebMessages.SoloHoy);
        (await sut.RegistrarAsync(vendedor, new RegistrarVentaWebRequest(Hoy, 0m, Clave), Hoy, CancellationToken.None))
            .Message.Should().Be(VentaRegistradaWebMessages.ValorInvalido);
        (await sut.RegistrarAsync(vendedor, new RegistrarVentaWebRequest(Hoy, 10.5m, Clave), Hoy, CancellationToken.None))
            .Message.Should().Be(VentaRegistradaWebMessages.ValorInvalido);
        (await db.VentasRegistradasWeb.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Registrar_permite_varias_ventas_del_mismo_dia_y_todas_suman()
    {
        await using var db = Nueva();
        var vendedor = await Vendedor(db, "Ana");
        var sut = new VentaRegistradaWebService(db, new HasherFalso());

        var primera = await sut.RegistrarAsync(vendedor, new RegistrarVentaWebRequest(Hoy, 10_000m, Clave), Hoy, CancellationToken.None);
        var segunda = await sut.RegistrarAsync(vendedor, new RegistrarVentaWebRequest(Hoy, 20_000m, Clave), Hoy, CancellationToken.None);
        var tercera = await sut.RegistrarAsync(vendedor, new RegistrarVentaWebRequest(Hoy, 20_000m, Clave), Hoy, CancellationToken.None);

        primera.IsSuccess.Should().BeTrue();
        segunda.IsSuccess.Should().BeTrue();
        tercera.IsSuccess.Should().BeTrue();
        (await db.VentasRegistradasWeb.CountAsync()).Should().Be(3);
        (await sut.TotalAsync(Hoy, Hoy, null, vendedor, CancellationToken.None)).Data!.Total.Should().Be(50_000m);
    }

    [Fact]
    public async Task Listar_propias_devuelve_solo_las_del_vendedor_de_la_mas_reciente_a_la_mas_antigua()
    {
        await using var db = Nueva();
        var vendedor = await Vendedor(db, "Ana");
        var otro = await Vendedor(db, "Luis");
        db.VentasRegistradasWeb.AddRange(
            Venta(vendedor, Hoy.AddDays(-2), 5_000m),
            Venta(vendedor, Hoy, 7_000m),
            Venta(vendedor, Hoy.AddDays(-1), 6_000m),
            Venta(otro, Hoy, 9_000m));
        await db.SaveChangesAsync();
        var sut = new VentaRegistradaWebService(db, new HasherFalso());

        var pagina = await sut.ListarPropiasAsync(vendedor, 1, 2, CancellationToken.None);

        pagina.Data!.TotalFilas.Should().Be(3);
        pagina.Data.Items.Select(i => i.FechaVenta).Should().Equal(Hoy, Hoy.AddDays(-1));
    }

    [Fact]
    public async Task Total_suma_el_rango_y_respeta_grupo_y_vendedor()
    {
        await using var db = Nueva();
        var grupoNorte = Guid.NewGuid();
        var ana = await Vendedor(db, "Ana", grupoNorte);
        var luis = await Vendedor(db, "Luis", grupoNorte);
        var sara = await Vendedor(db, "Sara", Guid.NewGuid());
        db.VentasRegistradasWeb.AddRange(
            Venta(ana, Hoy, 10_000m),
            Venta(luis, Hoy.AddDays(-1), 20_000m),
            Venta(sara, Hoy, 40_000m),
            Venta(ana, Hoy.AddDays(-10), 80_000m));
        await db.SaveChangesAsync();
        var sut = new VentaRegistradaWebService(db, new HasherFalso());

        var todo = await sut.TotalAsync(Hoy.AddDays(-1), Hoy, null, null, CancellationToken.None);
        var norte = await sut.TotalAsync(Hoy.AddDays(-1), Hoy, grupoNorte, null, CancellationToken.None);
        var soloAna = await sut.TotalAsync(Hoy.AddDays(-1), Hoy, grupoNorte, ana, CancellationToken.None);
        var anaFueraDelGrupo = await sut.TotalAsync(Hoy.AddDays(-1), Hoy, Guid.NewGuid(), ana, CancellationToken.None);

        todo.Data!.Total.Should().Be(70_000m);
        todo.Data.Cantidad.Should().Be(3);
        norte.Data!.Total.Should().Be(30_000m);
        soloAna.Data!.Total.Should().Be(10_000m);
        anaFueraDelGrupo.Data!.Total.Should().Be(0m);
    }

    [Fact]
    public async Task Total_suma_aparte_los_juegos_individuales_y_combo_del_rango_y_grupo()
    {
        await using var db = Nueva();
        var grupoNorte = Guid.NewGuid();
        var ana = await Vendedor(db, "Ana", grupoNorte);
        var sara = await Vendedor(db, "Sara", Guid.NewGuid());
        db.RegistrosJuegoManual.AddRange(
            Individual(ana, Hoy.ToDateTime(new TimeOnly(23, 30)), 1_000m, 2_000m),
            Individual(sara, Hoy.ToDateTime(new TimeOnly(9, 0)), 4_000m),
            Individual(ana, Hoy.AddDays(-5).ToDateTime(new TimeOnly(9, 0)), 8_000m));
        db.RegistrosJuegoNumerosWebManual.AddRange(
            Combo(ana, Hoy.AddDays(-1).ToDateTime(new TimeOnly(0, 10)), 10_000m),
            Combo(sara, Hoy.ToDateTime(new TimeOnly(12, 0)), 20_000m),
            Combo(ana, Hoy.AddDays(1).ToDateTime(new TimeOnly(0, 5)), 40_000m));
        db.VentasRegistradasWeb.Add(Venta(ana, Hoy, 7_000m));
        await db.SaveChangesAsync();
        var sut = new VentaRegistradaWebService(db, new HasherFalso());

        var todo = await sut.TotalAsync(Hoy.AddDays(-1), Hoy, null, null, CancellationToken.None);
        var norte = await sut.TotalAsync(Hoy.AddDays(-1), Hoy, grupoNorte, null, CancellationToken.None);

        todo.Data!.Total.Should().Be(7_000m);
        todo.Data.TotalJuegos.Should().Be(37_000m);
        todo.Data.CantidadJuegos.Should().Be(4);
        norte.Data!.TotalJuegos.Should().Be(13_000m);
        norte.Data.CantidadJuegos.Should().Be(2);
    }

    private static RegistroJuegoManual Individual(Guid vendedor, DateTime fecha, params decimal[] valores)
    {
        var registro = new RegistroJuegoManual { RegistroId = Guid.NewGuid(), UsuarioId = vendedor, FechaRegistro = fecha, CodigoPublico = "A1", IdempotencyKey = Guid.NewGuid().ToString("N") };
        foreach (var valor in valores)
        {
            registro.Filas.Add(new FilaRegistroJuegoManual { FilaId = Guid.NewGuid(), RegistroId = registro.RegistroId, LoteriaId = Guid.NewGuid(), Numero = "1234", Valor = valor });
        }

        return registro;
    }

    private static RegistroJuegoNumerosWebManual Combo(Guid vendedor, DateTime fecha, decimal valor) => new()
    {
        RegistroId = Guid.NewGuid(),
        UsuarioId = vendedor,
        FechaRegistro = fecha,
        CodigoPublico = "C1",
        ValorJugado = valor
    };

    private static VentaRegistradaWeb Venta(Guid vendedor, DateOnly fecha, decimal valor) => new()
    {
        VentaRegistradaWebId = Guid.NewGuid(),
        VendedorId = vendedor,
        FechaVenta = fecha,
        Valor = valor,
        FechaRegistro = DateTime.UtcNow
    };

    private static async Task<Guid> Vendedor(NewRichDbContext db, string nombre, Guid? grupoId = null)
    {
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = nombre,
            NombreUsuario = nombre.ToLowerInvariant(),
            PasswordHash = Clave,
            PasswordSalt = "sal",
            Rol = RolUsuario.VendedorWeb
        };
        if (grupoId.HasValue)
        {
            usuario.UsuarioGrupos.Add(new UsuarioGrupo { UsuarioId = usuario.UsuarioId, GrupoId = grupoId.Value });
        }

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        return usuario.UsuarioId;
    }

    private static NewRichDbContext Nueva()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NewRichDbContext(options);
    }

    private sealed class HasherFalso : IPasswordHasher
    {
        public (string Hash, string Salt) Hash(string password) => (password, "sal");

        public bool Verify(string password, string hash, string salt) => password == hash;
    }
}
