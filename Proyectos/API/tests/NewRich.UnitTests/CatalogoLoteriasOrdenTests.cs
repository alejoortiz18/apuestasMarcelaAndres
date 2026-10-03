using FluentAssertions;
using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Services;
using NewRich.Domain.Enums;

namespace NewRich.UnitTests;

public sealed class CatalogoLoteriasOrdenTests
{
    [Fact]
    public void Ordena_por_hora_de_cierre_ascendente()
    {
        var items = new[]
        {
            Item("Noche", "22:00", EstadoGeneral.Activo),
            Item("Manana", "11:00", EstadoGeneral.Inactivo)
        };

        var ordenadas = CatalogoLoteriasOrden.Aplicar(items, null, "horaFin", "asc");

        ordenadas.Select(l => l.Nombre).Should().Equal("Manana", "Noche");
    }

    [Fact]
    public void Ordena_por_estado_descendente()
    {
        var items = new[]
        {
            Item("A", "10:00", EstadoGeneral.Activo),
            Item("B", "10:00", EstadoGeneral.Inactivo)
        };

        var ordenadas = CatalogoLoteriasOrden.Aplicar(items, null, "estado", "desc");

        ordenadas.Select(l => l.Estado).Should().Equal(EstadoGeneral.Inactivo, EstadoGeneral.Activo);
    }

    [Fact]
    public void Ordena_horario_habilitado_por_hora_de_cierre_no_por_apertura()
    {
        var items = new[]
        {
            Item("Tarde", "15:30", EstadoGeneral.Activo, horaInicio: "06:00", jornada: "Tarde"),
            Item("Manana", "09:30", EstadoGeneral.Activo, horaInicio: "10:00", jornada: "Mañana")
        };

        CatalogoLoteriasOrden.Aplicar(items, null, "horaFin", "asc")
            .Select(l => l.Nombre).Should().Equal("Manana", "Tarde");
    }

    [Fact]
    public void Ordena_por_nombre_y_jornada()
    {
        var items = new[]
        {
            Item("valle", "22:00", EstadoGeneral.Activo, jornada: "Noche"),
            Item("Bogotá", "22:30", EstadoGeneral.Activo, jornada: "Noche"),
            Item("Cafetero", "11:30", EstadoGeneral.Activo, jornada: "Mañana")
        };

        CatalogoLoteriasOrden.Aplicar(items, null, "nombre", "asc")
            .Select(l => l.Nombre).Should().Equal("Bogotá", "Cafetero", "valle");
        CatalogoLoteriasOrden.Aplicar(items, null, "jornada", "asc")
            .Select(l => l.Nombre).Should().Equal("Cafetero", "Bogotá", "valle");
        CatalogoLoteriasOrden.SiguienteDireccion("horaFin", null, null).Should().Be("asc");
        CatalogoLoteriasOrden.SiguienteDireccion("horaFin", "horaFin", "asc").Should().Be("desc");
    }

    [Fact]
    public void Filtra_por_hora_de_fin_de_la_jornada()
    {
        var items = new[]
        {
            Item("Dia", "11:00", EstadoGeneral.Activo),
            Item("Noche", "21:00", EstadoGeneral.Activo)
        };

        CatalogoLoteriasOrden.Aplicar(items, "Mañana", null, null).Should().ContainSingle(l => l.Nombre == "Dia");
    }

    private static LoteriaResponse Item(
        string nombre,
        string horaFin,
        EstadoGeneral estado,
        Guid? jornadaId = null,
        string? horaInicio = null,
        string? jornada = null) => new()
    {
        Nombre = nombre,
        HoraFin = horaFin,
        HoraInicio = horaInicio,
        Estado = estado,
        JornadaId = jornadaId,
        JornadaNombre = jornada
    };
}
