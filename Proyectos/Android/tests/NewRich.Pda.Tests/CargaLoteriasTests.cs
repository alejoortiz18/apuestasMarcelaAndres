using FluentAssertions;
using NewRich.Application.Contracts.Loterias;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Pda.Tests;

public sealed class CargaLoteriasTests
{
    private static LoteriaResponse Loteria(Guid id, string nombre) =>
        new() { LoteriaId = id, Nombre = nombre };

    [Fact]
    public void Sin_internet_no_se_consulta_la_api()
    {
        CargaLoterias.DebeConsultarApi(false).Should().BeFalse();
        CargaLoterias.DebeConsultarApi(true).Should().BeTrue();
    }

    [Fact]
    public void La_espera_de_la_api_es_corta_para_no_dejar_la_pantalla_en_blanco()
    {
        CargaLoterias.MsEspera.Should().BeLessThan(6000);
    }

    [Fact]
    public void Reconoce_cuando_las_loterias_no_cambiaron()
    {
        var uno = Guid.NewGuid();
        var dos = Guid.NewGuid();

        CargaLoterias.SonIguales(
            [Loteria(uno, "Boyacá"), Loteria(dos, "Cruz Roja")],
            [Loteria(uno, "Boyacá"), Loteria(dos, "Cruz Roja")]).Should().BeTrue();
    }

    [Fact]
    public void Reconoce_cuando_las_loterias_cambiaron()
    {
        var uno = Guid.NewGuid();
        var dos = Guid.NewGuid();

        CargaLoterias.SonIguales([Loteria(uno, "Boyacá")], [Loteria(dos, "Cruz Roja")]).Should().BeFalse();
        CargaLoterias.SonIguales([Loteria(uno, "Boyacá")], []).Should().BeFalse();
        CargaLoterias.SonIguales([], []).Should().BeTrue();
    }
}
