using FluentAssertions;
using NewRich.Application.Contracts.Kpi;
using NewRich.Application.Contracts.PremiosEntregadosWeb;
using NewRich.Application.Contracts.RevisadaManual;
using NewRich.Application.Contracts.Utilidades;
using NewRich.Application.Informes;

namespace NewRich.UnitTests;

public class InformeEgresosLecturaTests
{
    [Fact]
    public void Los_premios_por_loteria_suman_observador_y_vendedor_web()
    {
        var informe = InformeEgresosLectura.Armar(
            [new KpiPremioLoteriaResponse { Loteria = "Cali", Valor = 200m, Cantidad = 1 }],
            [Premio("Cali", 50m), Premio("Bogotá", 120m)],
            [],
            [],
            new DateOnly(2026, 11, 8));

        informe.PremiosPorLoteria.Select(p => (p.Texto, p.Valor, p.Cantidad))
            .Should().Equal(("Cali", 250m, 2), ("Bogotá", 120m, 1));
        informe.PremiosPorLoteria.Sum(p => p.Valor).Should().Be(370m);
        informe.Premios.Should().HaveCount(2);
    }

    [Fact]
    public void Los_gastos_y_el_cubierto_se_parten_como_en_el_informe()
    {
        var informe = InformeEgresosLectura.Armar(
            [new KpiPremioLoteriaResponse { Loteria = "chontico", Valor = 1_000_000m, Cantidad = 1 }],
            [Premio("Bogotá", 1_200_000m, "4545", 5_000m, "maria jose")],
            [
                new GastoPorTipoResponse("NOMINA", 1_200_000m, 1),
                new GastoPorTipoResponse("Alimentación", 20_000m, 1),
                new GastoPorTipoResponse("ARRIENDOS", 750_000m, 1)
            ],
            [
                new GrupoRevisadaManualResponse
                {
                    Loteria = "Cali",
                    Numero = "1234",
                    Subtotal = 350m,
                    FechaOperacion = new DateTime(2026, 11, 5, 9, 0, 0),
                    CubiertoPor = "Hugo",
                    DescuentoPorcentaje = 10m,
                    DescuentoValor = 42m,
                    TotalPagar = 378m
                }
            ],
            new DateOnly(2026, 11, 8));

        informe.PremiosPorLoteria.Select(p => (p.Texto, p.Valor, p.Porcentaje))
            .Should().Equal(("Bogotá", 1_200_000m, 54.5m), ("chontico", 1_000_000m, 45.5m));
        informe.GastosPorTipo.Select(g => (g.Texto, g.Valor, g.Porcentaje))
            .Should().Equal(("NOMINA", 1_200_000m, 60.9m), ("ARRIENDOS", 750_000m, 38.1m), ("Alimentación", 20_000m, 1.0m));
        informe.CubiertoPorLoteria.Select(c => (c.Texto, c.Valor, c.Cantidad, c.Porcentaje))
            .Should().Equal(("Cali", 350m, 1, 100m));
        informe.Premios.Single().Numero.Should().Be("4545");
        informe.Cubiertos.Single().CubiertoPor.Should().Be("Hugo");
    }

    private static PremioEntregadoWebResponse Premio(string loteria, decimal premio) =>
        Premio(loteria, premio, "1111", 10m, "web");

    private static PremioEntregadoWebResponse Premio(string loteria, decimal premio, string numero, decimal apostado, string vendedor) =>
        new(Guid.NewGuid(), new DateOnly(2026, 11, 5), loteria, numero, apostado, premio, DateTime.UtcNow, Guid.NewGuid(), vendedor);
}
