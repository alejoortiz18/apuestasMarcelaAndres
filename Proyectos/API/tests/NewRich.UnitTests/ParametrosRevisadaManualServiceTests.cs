using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class ParametrosRevisadaManualServiceTests
{
    [Fact]
    public async Task Guardar_conserva_comision_y_descuento_en_su_propia_tabla()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new NewRichDbContext(options);
        var sut = new ParametrosRevisadaManualService(db);

        var vacio = await sut.ObtenerAsync(CancellationToken.None);
        vacio.IsSuccess.Should().BeTrue();
        vacio.Data!.ComisionPorcentaje.Should().BeNull();
        vacio.Data.DescuentoPorcentaje.Should().BeNull();

        var comision = await sut.GuardarComisionAsync(50, CancellationToken.None);
        var descuento = await sut.GuardarDescuentoAsync(10, CancellationToken.None);

        comision.IsSuccess.Should().BeTrue(comision.Message);
        descuento.IsSuccess.Should().BeTrue(descuento.Message);
        var guardado = await sut.ObtenerAsync(CancellationToken.None);
        guardado.Data!.ComisionPorcentaje.Should().Be(50);
        guardado.Data.DescuentoPorcentaje.Should().Be(10);
        (await db.ParametrosRevisadaManual.CountAsync()).Should().Be(1);
        (await db.OperacionesRevisadaManual.CountAsync()).Should().Be(0);

        (await sut.GuardarComisionAsync(100, CancellationToken.None)).Message
            .Should().Be(RevisadaManualMessages.ParametroPorcentajeInvalido);
        (await sut.ObtenerAsync(CancellationToken.None)).Data!.ComisionPorcentaje.Should().Be(50);
    }
}
