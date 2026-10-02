using FluentAssertions;
using NewRich.Application.Contracts.Loterias;
using NewRich.Application.Services;
using NewRich.Domain.Enums;

namespace NewRich.UnitTests;

public sealed class ResumenLoteriasOrdenTests
{
    [Fact]
    public void Ordena_por_nombre_ascendente()
    {
        var items = new[] { Item("valle"), Item("Bogotá"), Item("Cauca") };

        var ordenadas = ResumenLoteriasOrden.Aplicar(items, "nombre", "asc");

        ordenadas.Select(l => l.Nombre).Should().Equal("Bogotá", "Cauca", "valle");
    }

    [Fact]
    public void Ordena_por_boletos_y_total_descendente()
    {
        var items = new[]
        {
            Item("A", boletos: 1, total: 300),
            Item("B", boletos: 5, total: 100),
            Item("C", boletos: 2, total: 200)
        };

        ResumenLoteriasOrden.Aplicar(items, "boletos", "desc")
            .Select(l => l.Nombre).Should().Equal("B", "C", "A");
        ResumenLoteriasOrden.Aplicar(items, "total", "desc")
            .Select(l => l.Nombre).Should().Equal("A", "C", "B");
    }

    [Fact]
    public void Ordena_por_hora_cierre_numero_tipo_y_estado()
    {
        var items = new[]
        {
            Item("A", horaFin: "22:00", numero: "9", tipo: "Combinado", estado: EstadoGeneral.Inactivo),
            Item("B", horaFin: "09:30", numero: "1", tipo: "Individual", estado: EstadoGeneral.Activo)
        };

        ResumenLoteriasOrden.Aplicar(items, "horacierre", "asc")
            .Select(l => l.Nombre).Should().Equal("B", "A");
        ResumenLoteriasOrden.Aplicar(items, "numero", "asc")
            .Select(l => l.Nombre).Should().Equal("B", "A");
        ResumenLoteriasOrden.Aplicar(items, "tipo", "asc")
            .Select(l => l.Nombre).Should().Equal("A", "B");
        ResumenLoteriasOrden.Aplicar(items, "estado", "asc")
            .Select(l => l.Estado).Should().Equal(EstadoGeneral.Activo, EstadoGeneral.Inactivo);
    }

    [Fact]
    public void La_primera_pulsacion_ordena_ascendente_y_la_segunda_descendente()
    {
        ResumenLoteriasOrden.SiguienteDireccion("boletos", null, null).Should().Be("asc");
        ResumenLoteriasOrden.SiguienteDireccion("boletos", "boletos", "asc").Should().Be("desc");
        ResumenLoteriasOrden.SiguienteDireccion("boletos", "boletos", "desc").Should().Be("asc");
        ResumenLoteriasOrden.SiguienteDireccion("nombre", "boletos", "desc").Should().Be("asc");
    }

    [Fact]
    public void Columna_desconocida_ordena_por_nombre()
    {
        var items = new[] { Item("Z"), Item("A") };

        ResumenLoteriasOrden.Aplicar(items, "desconocida", "desc")
            .Select(l => l.Nombre).Should().Equal("Z", "A");
    }

    private static LoteriaResponse Item(
        string nombre,
        int boletos = 0,
        decimal total = 0,
        string? horaFin = null,
        string? numero = null,
        string? tipo = null,
        EstadoGeneral estado = EstadoGeneral.Activo) => new()
    {
        Nombre = nombre,
        BoletosVendidos = boletos,
        TotalVendido = total,
        HoraFin = horaFin,
        NumeroJugado = numero,
        TipoApuesta = tipo,
        Estado = estado
    };
}
