using FluentAssertions;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Pda.Tests;

public sealed class NumerosRestringidosPdaTests
{
    [Fact]
    public void Usa_la_lista_del_servidor_cuando_hay_conexion()
    {
        NumerosRestringidosPda.Vigentes(["1234"], ["0001"]).Should().BeEquivalentTo(["1234"]);
    }

    [Fact]
    public void Una_lista_vacia_del_servidor_libera_los_numeros()
    {
        NumerosRestringidosPda.Vigentes([], ["0001"]).Should().BeEmpty();
    }

    [Fact]
    public void Arma_la_tabla_en_filas_del_ancho_pedido()
    {
        var tabla = NumerosRestringidosPda.EnFilas(["0001", "0002", "0003", "0004", "0005"], 3);

        tabla.Should().HaveCount(2);
        tabla[0].Should().Equal("0001", "0002", "0003");
        tabla[1].Should().Equal("0004", "0005");
    }

    [Fact]
    public void Sin_numeros_no_hay_tabla()
    {
        NumerosRestringidosPda.EnFilas([], 3).Should().BeEmpty();
    }

    [Fact]
    public void Sin_conexion_usa_los_numeros_guardados_en_el_dispositivo()
    {
        NumerosRestringidosPda.Vigentes(null, ["0001", "1234"]).Should().BeEquivalentTo(["0001", "1234"]);
        NumerosRestringidosPda.Vigentes(null, null).Should().BeEmpty();
    }
}
