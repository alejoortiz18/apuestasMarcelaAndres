using FluentAssertions;
using NewRich.Pda.Core;

namespace NewRich.Pda.Tests;

public sealed class ObservadorTextsTests
{
    [Fact]
    public void Textos_de_consulta_por_recibo_resultados_consultas_y_kpi()
    {
        PdaTexts.ValidarCodigoVenta.Should().Be("Validar código de venta");
        PdaTexts.ValidarCodigoVentaAyuda.Should().Be("Escribe el código impreso en el recibo.");
        PdaTexts.ReciboDeVenta.Should().Be("Recibo de venta");
        PdaTexts.ResultadosTitulo.Should().Be("Resultados y números ganadores");
        PdaTexts.ResultadosSoloLectura.Should().Be("Solo lectura");
        PdaTexts.Consultas.Should().Be("Consultas");
        PdaTexts.Validar.Should().Be("Validar");
        PdaTexts.ConsultasAyuda.Should().Be("Consulta boletos, ventas, vendedores y dispositivos.");
        PdaTexts.Kpi.Should().Be("KPI");
        PdaTexts.KpiSoloLectura.Should().Be("Los indicadores son de solo lectura.");
        PdaTexts.KpiSinMovimientos.Should().Be("No hay movimientos de venta en el periodo seleccionado.");
        PdaTexts.ConsultaBoletos.Should().Be("Boletos");
        PdaTexts.ConsultaVentas.Should().Be("Ventas");
        PdaTexts.ConsultaVendedores.Should().Be("Vendedores");
        PdaTexts.ConsultaDispositivos.Should().Be("Dispositivos");
        PdaTexts.ConsultaResultados.Should().Be("Resultados");
        PdaTexts.ConsultaConfiguracion.Should().Be("Configuración");
        PdaTexts.HoraAperturaPda.Should().Be("Hora de apertura del PDA");
        PdaTexts.HoraCierrePda.Should().Be("Hora de cierre del PDA");
        PdaTexts.FiltroGeneral.Should().Be("General");
    }

    [Fact]
    public void Textos_de_la_pantalla_de_vendedores_del_observador()
    {
        PdaTexts.VendedoresAcciones.Should().Be("Vendedores");
        PdaTexts.DesbloquearVendedor.Should().Be("Desbloquear");
        PdaTexts.RestablecerContrasenaVendedor.Should().Be("Restablecer contraseña");
        PdaTexts.Aceptar.Should().Be("Aceptar");
        PdaTexts.SiDesbloquear.Should().Be("Sí, desbloquear");
        PdaTexts.SiRestablecer.Should().Be("Sí, restablecer");
    }

    [Fact]
    public void Antes_de_desbloquear_o_restablecer_se_pregunta_nombrando_al_vendedor()
    {
        PdaTexts.ConfirmarDesbloqueoVendedor("Ana Ruiz")
            .Should().Be("¿Desbloquear a Ana Ruiz? Podrá volver a ingresar al PDA.");
        PdaTexts.ConfirmarRestablecerVendedor("Ana Ruiz")
            .Should().Be("¿Restablecer la contraseña de Ana Ruiz? La contraseña actual dejará de servir y la temporal se mostrará una sola vez.");
    }
}
