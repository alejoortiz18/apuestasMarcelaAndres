using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Services;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class NumerosRestringidosServiceTests
{
    private static readonly DateTime Ahora = new(2026, 9, 9, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Agregar_guarda_el_numero_y_evita_duplicados()
    {
        var sut = CreateSut();

        var creado = await sut.AgregarAsync("1234", CancellationToken.None);
        var duplicado = await sut.AgregarAsync(" 1234 ", CancellationToken.None);
        var listado = await sut.ListarAsync(CancellationToken.None);

        creado.IsSuccess.Should().BeTrue();
        creado.Data!.Numero.Should().Be("1234");
        duplicado.IsSuccess.Should().BeFalse();
        duplicado.Message.Should().Be(ConfiguracionMessages.NumeroRestringidoDuplicado);
        listado.Data.Should().ContainSingle(x => x.Numero == "1234");
    }

    [Fact]
    public async Task Agregar_rechaza_formato_invalido()
    {
        var sut = CreateSut();

        var result = await sut.AgregarAsync("12", CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(ValidationMessages.NumeroApuestaFormato);
    }

    [Fact]
    public async Task Eliminar_quita_el_numero_de_la_lista()
    {
        var sut = CreateSut();
        var creado = await sut.AgregarAsync("0001", CancellationToken.None);

        var eliminado = await sut.EliminarAsync(creado.Data!.NumeroRestringidoId, CancellationToken.None);
        var listado = await sut.ListarAsync(CancellationToken.None);

        eliminado.IsSuccess.Should().BeTrue();
        listado.Data.Should().BeEmpty();
    }

    private static NumerosRestringidosService CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new NumerosRestringidosService(new NewRichDbContext(options), new RelojFijo(Ahora));
    }

    private sealed class RelojFijo : IClock
    {
        public RelojFijo(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }
}
