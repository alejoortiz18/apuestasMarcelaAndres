using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.PremiosEntregadosWeb;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class PremioEntregadoWebServiceTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 22);
    private const string Clave = "secreta";

    [Fact]
    public async Task Registrar_guarda_el_premio_con_todos_sus_datos()
    {
        await using var db = Nueva();
        var vendedor = await Vendedor(db, "Ana");
        var loteria = await Loteria(db, "Boyacá");
        var sut = new PremioEntregadoWebService(db, new HasherFalso());

        var guardado = await sut.RegistrarAsync(vendedor, Solicitud(loteria), Hoy, CancellationToken.None);

        guardado.IsSuccess.Should().BeTrue(guardado.Message);
        guardado.StatusCode.Should().Be(201);
        guardado.Data!.Loteria.Should().Be("Boyacá");
        var fila = await db.PremiosEntregadosWeb.SingleAsync();
        fila.VendedorId.Should().Be(vendedor);
        fila.LoteriaId.Should().Be(loteria);
        fila.Numero.Should().Be("0457");
        fila.ValorApostado.Should().Be(2_000m);
        fila.ValorPremio.Should().Be(900_000m);
        fila.FechaPremio.Should().Be(Hoy.AddDays(-1));
    }

    [Fact]
    public async Task Registrar_pide_la_contrasena_correcta()
    {
        await using var db = Nueva();
        var vendedor = await Vendedor(db, "Ana");
        var loteria = await Loteria(db, "Boyacá");
        var sut = new PremioEntregadoWebService(db, new HasherFalso());

        (await sut.RegistrarAsync(vendedor, Solicitud(loteria) with { Password = "otra" }, Hoy, CancellationToken.None))
            .Message.Should().Be(PremioEntregadoWebMessages.ContrasenaIncorrecta);
        (await sut.RegistrarAsync(vendedor, Solicitud(loteria) with { Password = "" }, Hoy, CancellationToken.None))
            .Message.Should().Be(PremioEntregadoWebMessages.ContrasenaRequerida);
        (await db.PremiosEntregadosWeb.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("", PremioEntregadoWebMessages.NumeroInvalido)]
    [InlineData("12345", PremioEntregadoWebMessages.NumeroInvalido)]
    [InlineData("12a", PremioEntregadoWebMessages.NumeroInvalido)]
    public async Task Registrar_valida_el_numero(string numero, string mensaje)
    {
        await using var db = Nueva();
        var vendedor = await Vendedor(db, "Ana");
        var loteria = await Loteria(db, "Boyacá");
        var sut = new PremioEntregadoWebService(db, new HasherFalso());

        (await sut.RegistrarAsync(vendedor, Solicitud(loteria) with { Numero = numero }, Hoy, CancellationToken.None))
            .Message.Should().Be(mensaje);
    }

    [Fact]
    public async Task Registrar_valida_valores_fecha_y_loteria()
    {
        await using var db = Nueva();
        var vendedor = await Vendedor(db, "Ana");
        var loteria = await Loteria(db, "Boyacá");
        var inactiva = await Loteria(db, "Vieja", EstadoGeneral.Inactivo);
        var sut = new PremioEntregadoWebService(db, new HasherFalso());

        (await sut.RegistrarAsync(vendedor, Solicitud(loteria) with { ValorApostado = 0 }, Hoy, CancellationToken.None))
            .Message.Should().Be(PremioEntregadoWebMessages.ValorApostadoInvalido);
        (await sut.RegistrarAsync(vendedor, Solicitud(loteria) with { ValorApostado = 999_001 }, Hoy, CancellationToken.None))
            .Message.Should().Be(PremioEntregadoWebMessages.ValorApostadoInvalido);
        (await sut.RegistrarAsync(vendedor, Solicitud(loteria) with { ValorPremio = 0 }, Hoy, CancellationToken.None))
            .Message.Should().Be(PremioEntregadoWebMessages.ValorPremioInvalido);
        (await sut.RegistrarAsync(vendedor, Solicitud(loteria) with { FechaPremio = Hoy.AddDays(1) }, Hoy, CancellationToken.None))
            .Message.Should().Be(PremioEntregadoWebMessages.FechaFutura);
        (await sut.RegistrarAsync(vendedor, Solicitud(inactiva), Hoy, CancellationToken.None))
            .Message.Should().Be(PremioEntregadoWebMessages.LoteriaNoDisponible);
        (await sut.RegistrarAsync(vendedor, Solicitud(Guid.NewGuid()), Hoy, CancellationToken.None))
            .Message.Should().Be(PremioEntregadoWebMessages.LoteriaNoDisponible);
        (await sut.RegistrarAsync(vendedor, Solicitud(loteria) with { FechaPremio = Hoy, ValorApostado = 999_000 }, Hoy, CancellationToken.None))
            .IsSuccess.Should().BeTrue();
        (await db.PremiosEntregadosWeb.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Listar_propios_devuelve_solo_los_del_vendedor_del_mas_reciente_al_mas_antiguo()
    {
        await using var db = Nueva();
        var vendedor = await Vendedor(db, "Ana");
        var otro = await Vendedor(db, "Luis");
        var loteria = await Loteria(db, "Boyacá");
        db.PremiosEntregadosWeb.AddRange(
            Premio(vendedor, loteria, Hoy.AddDays(-2), 1_000m),
            Premio(vendedor, loteria, Hoy, 3_000m),
            Premio(vendedor, loteria, Hoy.AddDays(-1), 2_000m),
            Premio(otro, loteria, Hoy, 9_000m));
        await db.SaveChangesAsync();
        var sut = new PremioEntregadoWebService(db, new HasherFalso());

        var pagina = await sut.ListarPropiosAsync(vendedor, 1, 2, CancellationToken.None);

        pagina.Data!.TotalFilas.Should().Be(3);
        pagina.Data.Items.Select(i => i.ValorPremio).Should().Equal(3_000m, 2_000m);
        pagina.Data.Items.Should().OnlyContain(i => i.Loteria == "Boyacá");
    }

    [Fact]
    public async Task Total_y_listado_respetan_rango_grupo_y_vendedor()
    {
        await using var db = Nueva();
        var norte = Guid.NewGuid();
        var ana = await Vendedor(db, "Ana", norte);
        var luis = await Vendedor(db, "Luis", norte);
        var sara = await Vendedor(db, "Sara", Guid.NewGuid());
        var loteria = await Loteria(db, "Boyacá");
        db.PremiosEntregadosWeb.AddRange(
            Premio(ana, loteria, Hoy, 10_000m),
            Premio(luis, loteria, Hoy.AddDays(-1), 20_000m),
            Premio(sara, loteria, Hoy, 40_000m),
            Premio(ana, loteria, Hoy.AddDays(-10), 80_000m));
        await db.SaveChangesAsync();
        var sut = new PremioEntregadoWebService(db, new HasherFalso());

        (await sut.TotalAsync(Hoy.AddDays(-1), Hoy, null, null, CancellationToken.None)).Data!.Should().Be(new TotalPremiosEntregadosWebResponse(70_000m, 3));
        (await sut.TotalAsync(Hoy.AddDays(-1), Hoy, norte, null, CancellationToken.None)).Data!.Total.Should().Be(30_000m);
        (await sut.TotalAsync(Hoy.AddDays(-1), Hoy, norte, ana, CancellationToken.None)).Data!.Total.Should().Be(10_000m);

        var listado = await sut.ListadoAsync(Hoy.AddDays(-1), Hoy, norte, CancellationToken.None);
        listado.Data!.Select(p => p.Vendedor).Should().BeEquivalentTo("Ana", "Luis");
    }

    private static RegistrarPremioWebRequest Solicitud(Guid loteria) =>
        new("0457", 2_000m, loteria, Hoy.AddDays(-1), 900_000m, Clave);

    private static PremioEntregadoWeb Premio(Guid vendedor, Guid loteria, DateOnly fecha, decimal valor) => new()
    {
        PremioEntregadoWebId = Guid.NewGuid(),
        VendedorId = vendedor,
        LoteriaId = loteria,
        Numero = "123",
        ValorApostado = 1_000m,
        ValorPremio = valor,
        FechaPremio = fecha,
        FechaRegistro = DateTime.UtcNow.AddMinutes(fecha.DayNumber)
    };

    private static async Task<Guid> Loteria(NewRichDbContext db, string nombre, EstadoGeneral estado = EstadoGeneral.Activo)
    {
        var loteria = new Loteria { LoteriaId = Guid.NewGuid(), Nombre = nombre, Estado = estado };
        db.Loterias.Add(loteria);
        await db.SaveChangesAsync();
        return loteria.LoteriaId;
    }

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
