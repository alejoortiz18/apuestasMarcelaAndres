using FluentAssertions;
using NewRich.Admin.Services;

namespace NewRich.Admin.Tests;

public sealed class ClavesProteccionDatosTests
{
    [Fact]
    public void En_iis_las_claves_quedan_fuera_de_la_carpeta_que_se_borra_al_publicar()
    {
        ClavesProteccionDatos.Resolver(null, esDesarrollo: false)
            .Should().Be(@"C:\inetpub\rich-keys");
    }

    [Fact]
    public void Una_ruta_configurada_se_respeta()
    {
        ClavesProteccionDatos.Resolver(@"D:\claves", esDesarrollo: false)
            .Should().Be(@"D:\claves");
    }

    [Fact]
    public void En_desarrollo_no_fuerza_la_carpeta_de_iis()
    {
        ClavesProteccionDatos.Resolver(null, esDesarrollo: true).Should().BeNull();
    }
}
