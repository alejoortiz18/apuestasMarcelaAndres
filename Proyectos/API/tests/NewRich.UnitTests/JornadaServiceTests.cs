using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Services;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class JornadaServiceTests
{
    [Fact]
    public async Task ListarAsync_devuelve_las_jornadas_en_orden_del_dia_con_su_cantidad_de_loterias()
    {
        var (sut, db) = CreateSut();
        AgregarJornada(db, "Noche");
        var manana = AgregarJornada(db, "Mañana");
        AgregarJornada(db, "Tarde");
        db.Loterias.AddRange(
            new Loteria { LoteriaId = Guid.NewGuid(), Nombre = "Boyaca", Estado = EstadoGeneral.Activo, JornadaId = manana.JornadaId, FechaCreacion = DateTime.UtcNow },
            new Loteria { LoteriaId = Guid.NewGuid(), Nombre = "Cauca", Estado = EstadoGeneral.Activo, JornadaId = manana.JornadaId, FechaCreacion = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Data!.Select(j => (j.Nombre, j.CantidadLoterias))
            .Should().Equal(("Mañana", 2), ("Tarde", 0), ("Noche", 0));
    }

    private static (JornadaService Sut, NewRichDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        return (new JornadaService(db), db);
    }

    private static Jornada AgregarJornada(NewRichDbContext db, string nombre)
    {
        var jornada = new Jornada { JornadaId = Guid.NewGuid(), Nombre = nombre, FechaCreacion = DateTime.UtcNow };
        db.Jornadas.Add(jornada);
        return jornada;
    }
}
