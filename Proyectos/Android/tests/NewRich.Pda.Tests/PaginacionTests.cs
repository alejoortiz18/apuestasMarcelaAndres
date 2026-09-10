using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class PaginacionTests
{
    [Fact]
    public void Pagina_no_supera_quince_filas()
    {
        var items = Enumerable.Range(1, 40).ToArray();

        var pagina = Paginacion.Pagina(items, 1, 20);

        pagina.Should().HaveCount(15);
        Paginacion.Resumen(1, 15, 40).Should().Be("Mostrando 1-15 de 40");
    }

    [Fact]
    public void Segunda_pagina_continua_desde_el_indice_correcto()
    {
        var items = Enumerable.Range(1, 12).ToArray();

        var pagina = Paginacion.Pagina(items, 2, 5);

        pagina.Should().Equal(6, 7, 8, 9, 10);
    }
}
