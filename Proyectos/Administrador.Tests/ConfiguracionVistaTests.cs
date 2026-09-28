using FluentAssertions;
using NewRich.Admin.Constants;

namespace NewRich.Admin.Tests;

public sealed class ConfiguracionVistaTests
{
    [Fact]
    public void El_horario_del_pda_queda_arriba_del_catalogo_de_loterias()
    {
        var vista = File.ReadAllText(RutaVista());
        var horarioPremios = vista.IndexOf("ConfigHorarioPremios", StringComparison.Ordinal);
        var catalogo = vista.IndexOf("CatalogoLoterias", StringComparison.Ordinal);
        var apertura = vista.IndexOf("Form.HoraApertura", StringComparison.Ordinal);
        var cierre = vista.IndexOf("Form.HoraCierre", StringComparison.Ordinal);
        var buscarCatalogo = vista.IndexOf("id=\"q\"", StringComparison.Ordinal);

        catalogo.Should().BePositive();
        apertura.Should().BeGreaterThan(catalogo);
        cierre.Should().BeGreaterThan(catalogo);
        buscarCatalogo.Should().BeGreaterThan(Math.Max(apertura, cierre));
        horarioPremios.Should().BePositive();
        apertura.Should().BeGreaterThan(horarioPremios);

        var bloqueHorario = vista[horarioPremios..catalogo];
        bloqueHorario.Should().NotContain("Form.HoraApertura");
        bloqueHorario.Should().NotContain("Form.HoraCierre");
        bloqueHorario.Should().Contain("Form.VigenciaPremiosDias");
    }

    [Fact]
    public void El_catalogo_muestra_el_horario_habilitado_de_cada_loteria()
    {
        var catalogo = File.ReadAllText(RutaVista());
        catalogo.Should().Contain("HorarioHabilitado");
        catalogo.Should().Contain("HoraInicio");
        catalogo.Should().Contain("HoraFin");
    }

    [Fact]
    public void El_formulario_de_loteria_pide_hora_inicio_y_fin()
    {
        var formulario = File.ReadAllText(Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Administrador", "Views", "Configuracion", "LoteriaForm.cshtml")));
        formulario.Should().Contain("HoraInicio");
        formulario.Should().Contain("HoraFin");
    }

    [Fact]
    public void El_formulario_pide_el_mensaje_al_superar_el_tope()
    {
        var vista = File.ReadAllText(RutaVista());
        vista.Should().Contain("Form.MensajeSuperacionTope");
        UiTexts.MensajeSuperacionTope.Should().Be("Mensaje al superar el tope");
    }

    [Fact]
    public void El_maximo_de_juegos_se_llama_modo_combo()
    {
        UiTexts.MaxJuegosCombinado.Should().Be("Juegos máximos en modo combo");
    }

    [Fact]
    public void La_retencion_queda_junto_a_codigos_offline()
    {
        var vista = File.ReadAllText(RutaVista());
        var offline = vista.IndexOf("ConfigOffline", StringComparison.Ordinal);
        var retencion = vista.IndexOf("ConfigRetencion", StringComparison.Ordinal);
        var tirilla = vista.IndexOf("ConfigTirilla", StringComparison.Ordinal);

        offline.Should().BePositive();
        retencion.Should().BeGreaterThan(offline);
        tirilla.Should().BeGreaterThan(retencion);
        vista.Should().Contain("meses-maximos-retencion");
        vista.Should().Contain("readonly disabled");
        vista.Should().Contain("Form.MesesAEliminar");
        vista.Should().NotContain("días a eliminar");
        UiTexts.ConfigRetencion.Should().Be("Retención de históricos");
        UiTexts.MesesAEliminar.Should().Be("Meses a eliminar");
    }

    private static string RutaVista()
    {
        var ruta = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Administrador", "Views", "Configuracion", "Index.cshtml"));
        File.Exists(ruta).Should().BeTrue($"se esperaba la vista en {ruta}");
        return ruta;
    }
}
