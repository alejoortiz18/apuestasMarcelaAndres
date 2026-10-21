using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Contracts.Informes;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class ConfiguracionInformeServiceTests
{
    private static readonly Guid Admin = Guid.NewGuid();
    private const string Filtros = """{"grupos":null,"usuarios":null,"tipos":[2]}""";

    [Fact]
    public async Task Guardar_crea_la_configuracion_y_listar_la_devuelve()
    {
        var (sut, _) = CreateSut();

        var guardado = await sut.GuardarAsync(Admin, CategoriasInforme.Vendedores, Pedido(Filtros), CancellationToken.None);
        var lista = await sut.ListarAsync(Admin, CancellationToken.None);

        guardado.IsSuccess.Should().BeTrue(guardado.Message);
        lista.Data!.Should().ContainSingle();
        lista.Data![0].Categoria.Should().Be(CategoriasInforme.Vendedores);
        lista.Data![0].Filtros.Should().Be(Filtros);
    }

    [Fact]
    public async Task Guardar_otra_vez_reemplaza_los_filtros_sin_duplicar()
    {
        var (sut, db) = CreateSut();
        await sut.GuardarAsync(Admin, CategoriasInforme.Vendedores, Pedido(Filtros), CancellationToken.None);

        await sut.GuardarAsync(Admin, CategoriasInforme.Vendedores, Pedido("""{"tipos":null}"""), CancellationToken.None);

        (await db.ConfiguracionesInforme.CountAsync()).Should().Be(1);
        (await db.ConfiguracionesInforme.SingleAsync()).Filtros.Should().Be("""{"tipos":null}""");
    }

    [Fact]
    public async Task Listar_solo_devuelve_las_del_administrador_que_consulta()
    {
        var (sut, _) = CreateSut();
        await sut.GuardarAsync(Guid.NewGuid(), CategoriasInforme.Vendedores, Pedido(Filtros), CancellationToken.None);

        var lista = await sut.ListarAsync(Admin, CancellationToken.None);

        lista.Data!.Should().BeEmpty();
    }

    [Fact]
    public async Task Quitar_elimina_la_categoria_del_informe_de_ese_administrador()
    {
        var (sut, db) = CreateSut();
        var otro = Guid.NewGuid();
        await sut.GuardarAsync(Admin, CategoriasInforme.Vendedores, Pedido(Filtros), CancellationToken.None);
        await sut.GuardarAsync(otro, CategoriasInforme.Vendedores, Pedido(Filtros), CancellationToken.None);

        var quitado = await sut.QuitarAsync(Admin, CategoriasInforme.Vendedores, CancellationToken.None);

        quitado.IsSuccess.Should().BeTrue(quitado.Message);
        (await db.ConfiguracionesInforme.Select(c => c.UsuarioId).ToListAsync()).Should().Equal(otro);
    }

    [Fact]
    public async Task Guardar_rechaza_una_categoria_que_no_esta_habilitada()
    {
        var (sut, db) = CreateSut();

        var guardado = await sut.GuardarAsync(Admin, "utilidades-kpi", Pedido(Filtros), CancellationToken.None);

        guardado.IsSuccess.Should().BeFalse();
        (await db.ConfiguracionesInforme.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no es json")]
    [InlineData("[1,2]")]
    public async Task Guardar_rechaza_filtros_que_no_son_un_objeto_json(string filtros)
    {
        var (sut, db) = CreateSut();

        var guardado = await sut.GuardarAsync(Admin, CategoriasInforme.Vendedores, Pedido(filtros), CancellationToken.None);

        guardado.IsSuccess.Should().BeFalse();
        (await db.ConfiguracionesInforme.CountAsync()).Should().Be(0);
    }

    private static GuardarConfiguracionInformeRequest Pedido(string filtros) => new() { Filtros = filtros };

    private static (ConfiguracionInformeService Sut, NewRichDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        return (new ConfiguracionInformeService(db), db);
    }
}
