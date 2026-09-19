using FluentAssertions;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Application.Contracts.Ventas;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class ConsultaObservadorTests
{
    [Fact]
    public void Las_ventas_permiten_filtrar_por_tipo_de_apuesta_y_fecha_de_venta()
    {
        var etiquetas = ConsultaObservadorCampos.Obtener(PdaTexts.ConsultaVentas).Select(x => x.Etiqueta).ToList();

        etiquetas.Should().Contain(PdaTexts.TipoApuestaFiltro);
        etiquetas.Should().Contain(PdaTexts.NumeroApostado);
        etiquetas.Should().Contain(PdaTexts.Loteria);
        etiquetas.Should().Contain(PdaTexts.Desde);
        etiquetas.Should().Contain(PdaTexts.Hasta);
    }

    [Fact]
    public void Los_vendedores_se_eligen_de_una_lista_y_no_escribiendo_el_nombre()
    {
        var etiquetas = ConsultaObservadorCampos.Obtener(PdaTexts.ConsultaVendedores).Select(x => x.Etiqueta).ToList();

        etiquetas.Should().Contain(PdaTexts.Grupo);
        etiquetas.Should().Contain(PdaTexts.Vendedor);
        etiquetas.Should().Contain(PdaTexts.Desde);
        etiquetas.Should().Contain(PdaTexts.Hasta);
        etiquetas.Should().NotContain(PdaTexts.Usuario);
    }

    [Fact]
    public void Los_dispositivos_conservan_sus_filtros_actuales()
    {
        var etiquetas = ConsultaObservadorCampos.Obtener(PdaTexts.ConsultaDispositivos).Select(x => x.Etiqueta).ToList();

        etiquetas.Should().Equal(PdaTexts.CodigoPda, PdaTexts.Conectado, PdaTexts.Grupo);
    }

    [Fact]
    public void Los_resultados_se_filtran_por_fecha_de_resultado_y_loteria()
    {
        var etiquetas = ConsultaObservadorCampos.Obtener(PdaTexts.ConsultaResultados).Select(x => x.Etiqueta).ToList();

        etiquetas.Should().Equal(PdaTexts.FechaResultado, PdaTexts.Loteria);
    }

    [Fact]
    public void El_verde_del_ganador_es_el_solicitado()
    {
        ConsultaObservadorResultados.ColorGanador.Should().Be("#a3f1ad");
    }

    [Fact]
    public void El_lila_del_premio_entregado_es_el_solicitado()
    {
        ConsultaObservadorResultados.ColorEntregado.Should().Be("#cfd0e3");
    }

    [Fact]
    public void Un_boleto_ganador_se_puede_seleccionar_aunque_el_premio_ya_se_haya_pagado()
    {
        ConsultaObservadorResultados.EsGanador(BoletoMessages.BoletoGanador).Should().BeTrue();
        ConsultaObservadorResultados.EsGanador(BoletoMessages.BoletoPagado).Should().BeTrue();
        ConsultaObservadorResultados.EsGanador(BoletoMessages.BoletoPremioEntregado).Should().BeTrue();
        ConsultaObservadorResultados.EsGanador("Ganador").Should().BeTrue();
        ConsultaObservadorResultados.EsGanador("PagadoCobrado").Should().BeTrue();
        ConsultaObservadorResultados.EsGanador("PremioEntregado").Should().BeTrue();
    }

    [Fact]
    public void Un_boleto_que_no_gano_no_se_resalta_ni_se_selecciona()
    {
        ConsultaObservadorResultados.EsGanador(BoletoMessages.BoletoNoGanador).Should().BeFalse();
        ConsultaObservadorResultados.EsGanador(BoletoMessages.BoletoJugado).Should().BeFalse();
        ConsultaObservadorResultados.EsGanador(BoletoMessages.BoletoPorJugar).Should().BeFalse();
        ConsultaObservadorResultados.EsGanador(BoletoMessages.BoletoVencido).Should().BeFalse();
        ConsultaObservadorResultados.EsGanador(null).Should().BeFalse();
    }

    [Fact]
    public void Solo_el_premio_entregado_usa_el_color_lila()
    {
        ConsultaObservadorResultados.EsPremioEntregado(BoletoMessages.BoletoPremioEntregado).Should().BeTrue();
        ConsultaObservadorResultados.EsPremioEntregado("PremioEntregado").Should().BeTrue();
        ConsultaObservadorResultados.EsPremioEntregado(BoletoMessages.BoletoGanador).Should().BeFalse();
        ConsultaObservadorResultados.EsPremioEntregado(BoletoMessages.BoletoPagado).Should().BeFalse();
        ConsultaObservadorResultados.EsPremioEntregado(null).Should().BeFalse();
    }

    [Fact]
    public void El_color_de_resaltado_distingue_ganador_y_entregado()
    {
        ConsultaObservadorResultados.ColorResaltado(BoletoMessages.BoletoGanador)
            .Should().Be(ConsultaObservadorResultados.ColorGanador);
        ConsultaObservadorResultados.ColorResaltado("Ganador")
            .Should().Be(ConsultaObservadorResultados.ColorGanador);
        ConsultaObservadorResultados.ColorResaltado(BoletoMessages.BoletoPremioEntregado)
            .Should().Be(ConsultaObservadorResultados.ColorEntregado);
        ConsultaObservadorResultados.ColorResaltado("PremioEntregado")
            .Should().Be(ConsultaObservadorResultados.ColorEntregado);
        ConsultaObservadorResultados.ColorResaltado(BoletoMessages.BoletoJugado).Should().BeNull();
    }

    [Fact]
    public void Una_venta_ganadora_se_puede_abrir_como_boleto_para_ver_el_detalle()
    {
        var venta = new VentaResponse
        {
            BoletoId = Guid.NewGuid(),
            CodigoPublico = "1234567",
            Vendedor = "Andrés Pérez",
            FechaVenta = new DateTime(2026, 9, 18, 15, 30, 0),
            Total = 5000m,
            EstadoBoleto = "PremioEntregado"
        };

        var boleto = ConsultaObservadorResultados.ComoBoleto(venta);

        boleto.BoletoId.Should().Be(venta.BoletoId);
        boleto.CodigoPublico.Should().Be(venta.CodigoPublico);
        boleto.Vendedor.Should().Be(venta.Vendedor);
        boleto.Fecha.Should().Be(venta.FechaVenta);
        boleto.Total.Should().Be(venta.Total);
        boleto.Estado.Should().Be(venta.EstadoBoleto);
        ConsultaObservadorResultados.EsGanador(boleto.Estado).Should().BeTrue();
    }

    [Fact]
    public void El_tipo_de_apuesta_de_la_venta_se_muestra_con_el_nombre_que_usa_el_pda()
    {
        ConsultaObservadorResultados.TipoApuestaTexto(TipoApuesta.COMBINADO).Should().Be(PdaTexts.TipoCombinada);
        ConsultaObservadorResultados.TipoApuestaTexto(TipoApuesta.INDIVIDUAL).Should().Be(PdaTexts.TipoIndividual);
    }

    [Fact]
    public void Los_numeros_apostados_de_una_venta_se_listan_sin_repetir()
    {
        var venta = new VentaResponse
        {
            Juegos =
            [
                new JuegoResponse { Numero = "42" },
                new JuegoResponse { Numero = "42" },
                new JuegoResponse { Numero = "07" }
            ]
        };

        ConsultaObservadorResultados.NumerosApostados(venta).Should().Be("42, 07");
    }

    [Fact]
    public void El_total_por_vendedor_suma_las_ventas_del_rango_consultado()
    {
        var pedro = Guid.NewGuid();
        var ana = Guid.NewGuid();
        var usuarios = new List<UsuarioResponse>
        {
            Vendedor(pedro, "Pedro", "Norte"),
            Vendedor(ana, "Ana", "Sur")
        };
        var ventas = new List<VentaResponse>
        {
            new() { VendedorId = pedro, Total = 3000m },
            new() { VendedorId = pedro, Total = 2000m },
            new() { VendedorId = ana, Total = 1000m }
        };

        var resumen = ConsultaObservadorResultados.TotalesPorVendedor(usuarios, ventas);

        resumen.Should().HaveCount(2);
        resumen[0].Vendedor.Should().Be("Pedro");
        resumen[0].Total.Should().Be(5000m);
        resumen[0].Ventas.Should().Be(2);
        resumen[1].Total.Should().Be(1000m);
    }

    [Fact]
    public void Un_vendedor_sin_ventas_en_el_rango_aparece_en_cero()
    {
        var sinVentas = Guid.NewGuid();
        var usuarios = new List<UsuarioResponse> { Vendedor(sinVentas, "Luis", "Norte") };

        var resumen = ConsultaObservadorResultados.TotalesPorVendedor(usuarios, []);

        resumen.Should().ContainSingle();
        resumen[0].Total.Should().Be(0m);
        resumen[0].Ventas.Should().Be(0);
    }

    [Fact]
    public void Solo_se_resumen_los_vendedores_y_no_los_demas_roles()
    {
        var usuarios = new List<UsuarioResponse>
        {
            Vendedor(Guid.NewGuid(), "Pedro", "Norte"),
            new() { UsuarioId = Guid.NewGuid(), NombreCompleto = "Admin", Rol = RolUsuario.Administrador }
        };

        var resumen = ConsultaObservadorResultados.TotalesPorVendedor(usuarios, []);

        resumen.Should().ContainSingle();
        resumen[0].Vendedor.Should().Be("Pedro");
    }

    [Fact]
    public void La_cantidad_de_ganadores_se_escribe_en_singular_o_plural()
    {
        PdaTexts.CantidadGanadores(0).Should().Be("0 ganadores");
        PdaTexts.CantidadGanadores(1).Should().Be("1 ganador");
        PdaTexts.CantidadGanadores(2).Should().Be("2 ganadores");
    }

    private static UsuarioResponse Vendedor(Guid id, string nombre, string grupo) => new()
    {
        UsuarioId = id,
        NombreCompleto = nombre,
        Rol = RolUsuario.Vendedor,
        GrupoNombre = grupo
    };
}
