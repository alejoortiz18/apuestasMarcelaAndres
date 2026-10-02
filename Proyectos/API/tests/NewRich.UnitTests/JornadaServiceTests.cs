using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Jornadas;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class JornadaServiceTests
{
    [Fact]
    public async Task No_elimina_una_jornada_con_loterias()
    {
        var (sut, db) = CreateSut();
        var jornada = await AgregarJornadaAsync(db, "Mañana");
        db.Loterias.Add(new Loteria
        {
            LoteriaId = Guid.NewGuid(),
            Nombre = "Boyaca",
            JornadaId = jornada.JornadaId,
            FechaCreacion = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await sut.EliminarAsync(jornada.JornadaId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(UsuarioMessages.JornadaConLoteriasAsociadas);
        db.Jornadas.Should().ContainSingle(j => j.JornadaId == jornada.JornadaId);
    }

    [Fact]
    public async Task Crea_una_jornada_solo_con_nombre()
    {
        var (sut, _) = CreateSut();

        var result = await sut.CrearAsync(new CrearJornadaRequest { Nombre = "  Extra  " }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Nombre.Should().Be("Extra");
    }

    private static (JornadaService Sut, NewRichDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        return (new JornadaService(db, new RelojFijo()), db);
    }

    private static async Task<Jornada> AgregarJornadaAsync(NewRichDbContext db, string nombre)
    {
        var jornada = new Jornada
        {
            JornadaId = Guid.NewGuid(),
            Nombre = nombre,
            FechaCreacion = DateTime.UtcNow
        };
        db.Jornadas.Add(jornada);
        await db.SaveChangesAsync();
        return jornada;
    }

    private sealed class RelojFijo : IClock
    {
        public DateTime UtcNow => new(2026, 10, 1, 17, 0, 0, DateTimeKind.Utc);
        public DateTime LocalNow => new(2026, 10, 1, 12, 0, 0);
    }
}
