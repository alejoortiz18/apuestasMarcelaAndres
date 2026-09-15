using FluentAssertions;
using NewRich.Pda.Core.Api;

namespace NewRich.Pda.Tests;

public sealed class IdentidadDispositivoTests
{
    [Fact]
    public void Usa_el_codigo_que_grabo_el_registro_del_administrador()
    {
        PdaConexion.Resolver("PDA-9F3A2B7C", "H10").Should().Be("PDA-9F3A2B7C");
    }

    [Fact]
    public void Dos_equipos_del_mismo_modelo_conservan_identidades_distintas()
    {
        var primero = PdaConexion.Resolver("PDA-0001AAAA", "H10");
        var segundo = PdaConexion.Resolver("PDA-0002BBBB", "H10");

        primero.Should().NotBe(segundo);
    }

    [Fact]
    public void Sin_registro_previo_vuelve_al_codigo_derivado_del_modelo()
    {
        PdaConexion.Resolver(null, "H10").Should().Be("CEL-H10");
        PdaConexion.Resolver("   ", "RMX3710").Should().Be("CEL-RMX3710");
    }

    [Fact]
    public void El_texto_null_del_ajuste_sin_valor_no_se_toma_como_codigo()
    {
        // "settings get" devuelve la palabra null cuando la clave no existe.
        PdaConexion.Resolver("null", "H10").Should().Be("CEL-H10");
    }

    [Fact]
    public void Normaliza_el_codigo_a_mayusculas_y_sin_espacios()
    {
        PdaConexion.Resolver(" pda-9f3a2b7c ", "H10").Should().Be("PDA-9F3A2B7C");
    }

    [Fact]
    public void Descarta_codigos_mas_largos_de_lo_que_admite_la_base()
    {
        var excedido = new string('A', PdaConexion.LargoMaximoCodigo + 1);

        PdaConexion.Resolver(excedido, "H10").Should().Be("CEL-H10");
    }

    [Fact]
    public void Descarta_codigos_con_caracteres_que_la_api_no_espera()
    {
        PdaConexion.Resolver("PDA 001", "H10").Should().Be("CEL-H10");
        PdaConexion.Resolver("PDA/001", "H10").Should().Be("CEL-H10");
    }

    [Fact]
    public void La_clave_del_ajuste_es_la_que_escribe_el_administrador()
    {
        PdaConexion.ClaveCodigoProvisionado.Should().Be("newrich_codigo_dispositivo");
    }
}
