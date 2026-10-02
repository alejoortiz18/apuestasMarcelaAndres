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
        var catalogo = vista.IndexOf("@UiTexts.CatalogoLoterias<", StringComparison.Ordinal);
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
        formulario.Should().Contain("data-hora-fin-jornada");
        formulario.Should().Contain("data-jornada-min");
        formulario.Should().Contain("data-jornada-max");
        formulario.Should().Contain("data-hora-fin-mensaje");
        formulario.Should().Contain("TryRangoHoraFin");
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

    [Fact]
    public void El_catalogo_separa_jornadas_horario_del_pda_y_horarios_de_loterias()
    {
        var vista = File.ReadAllText(RutaVista());
        var catalogo = vista.IndexOf("id=\"catalogo-loterias\"", StringComparison.Ordinal);
        var jornadas = vista.IndexOf("id=\"catalogo-jornadas\"", StringComparison.Ordinal);
        var pda = vista.IndexOf("id=\"catalogo-horario-pda\"", StringComparison.Ordinal);
        var horarios = vista.IndexOf("id=\"catalogo-horarios-loterias\"", StringComparison.Ordinal);
        var agregarJornada = vista.IndexOf("AgregarJornada", catalogo, StringComparison.Ordinal);
        var guardarPda = vista.IndexOf("form=\"config-operativa\"", pda, StringComparison.Ordinal);
        var guardarHorarios = vista.IndexOf("GuardarHorarios", horarios, StringComparison.Ordinal);

        catalogo.Should().BePositive();
        jornadas.Should().BeGreaterThan(catalogo);
        pda.Should().BeGreaterThan(jornadas);
        horarios.Should().BeGreaterThan(pda);
        agregarJornada.Should().Be(-1);
        guardarPda.Should().BeGreaterThan(pda).And.BeLessThan(horarios);
        guardarHorarios.Should().BeGreaterThan(horarios);
        vista.Should().Contain("HorarioActividadPda");
        vista.Should().Contain("HorariosDeLoterias");
        var bloquePda = vista[pda..horarios];
        bloquePda.IndexOf("Form.HoraApertura", StringComparison.Ordinal)
            .Should().BePositive()
            .And.BeLessThan(bloquePda.IndexOf("Form.HoraCierre", StringComparison.Ordinal));
        vista.Should().Contain("data-original");
        vista.Should().Contain("data-guardar-horarios");
        vista.Should().Contain("disabled=\"disabled\"");
        var js = File.ReadAllText(RutaSiteJs());
        js.Should().Contain("data-original");
        js.Should().Contain("is-horario-cambio");
        js.Should().Contain("addEventListener(\"input\"");
        js.Should().Contain("data-guardar-horarios");
        js.Should().Contain("horarios[' + indice + '].LoteriaId");
        js.Should().Contain("hasAttribute(\"data-horarios-catalogo\")");
        js.Should().Contain("botonHorarios.disabled");
        vista.Should().Contain("data-horarios-guardados");
        var envio = js[js.IndexOf("formHorarios.addEventListener(\"submit\"", StringComparison.Ordinal)..];
        envio.Should().NotContain("sessionStorage.removeItem(KEY_HORARIOS)");
        js.Should().Contain("getAttribute(\"data-horarios-guardados\") === \"1\"");
        js.Should().NotContain("hasAttribute(\"data-horarios-guardados\")");
        var bloqueJornadas = vista[jornadas..pda];
        bloqueJornadas.Should().NotContain("<table>");
        bloqueJornadas.Should().Contain("catalogo-jornada-cortes");
        bloqueJornadas.Should().NotContain("AgregarJornada");
        vista.Should().NotContain("asp-action=\"CrearJornada\"");
        vista.Should().NotContain("asp-action=\"EditarJornada\"");
        vista.Should().NotContain("asp-action=\"EliminarJornada\"");
    }

    [Fact]
    public void Al_guardar_se_conserva_el_lugar_donde_se_estaba_trabajando()
    {
        var js = File.ReadAllText(RutaSiteJs());
        js.Should().Contain("nr.scroll.");
        js.Should().Contain("protegidoOk");
        js.Should().Contain("querySelector(\"main\")");
        js.Should().Contain("scrollTop");
        js.Should().Contain("history.scrollRestoration");
        js.Should().Contain("preventScroll");
        js.Should().Contain("pageshow");
        js.Should().Contain("button[type='submit']");
        js.Should().Contain("url.pathname !== location.pathname");
        js.Should().NotContain("!== \"post\"");
    }

    [Fact]
    public void Los_topes_filtran_por_jornada_con_pestanas()
    {
        var vista = File.ReadAllText(RutaVista());
        var js = File.ReadAllText(RutaSiteJs());
        var titulo = vista.IndexOf("ConfigTopes", StringComparison.Ordinal);
        var sub = vista.IndexOf("ConfigTopesSub", StringComparison.Ordinal);
        var tabs = vista.IndexOf("data-topes-jornadas", StringComparison.Ordinal);
        var tabla = vista.IndexOf("class=\"topes-tabla\"", StringComparison.Ordinal);
        var catalogo = vista.IndexOf("id=\"catalogo-loterias\"", StringComparison.Ordinal);

        titulo.Should().BePositive();
        sub.Should().BeGreaterThan(titulo);
        tabs.Should().BeGreaterThan(sub);
        tabla.Should().BeGreaterThan(tabs);
        tabs.Should().BeLessThan(catalogo);
        var manana = vista.IndexOf("UiTexts.Manana", tabs, StringComparison.Ordinal);
        var tarde = vista.IndexOf("UiTexts.Tarde", tabs, StringComparison.Ordinal);
        var noche = vista.IndexOf("UiTexts.Noche", tabs, StringComparison.Ordinal);
        var todas = vista.IndexOf("UiTexts.TodasLasJornadas", tabs, StringComparison.Ordinal);
        manana.Should().BeGreaterThan(tabs).And.BeLessThan(tarde);
        tarde.Should().BeLessThan(noche);
        noche.Should().BeLessThan(todas);
        todas.Should().BeLessThan(tabla);
        vista.Should().Contain("role=\"tablist\"");
        vista.Should().Contain("data-hora-fin-jornada");
        vista.Should().NotContain("JornadaTopeTabs");
        js.Should().Contain("iniciarFiltroHoraFin");
        js.Should().Contain("data-hora-fin-jornada");
        js.Should().Contain("iniciarHorarioFinJornada");
        js.Should().Contain("form[data-hora-fin-jornada]");
        js.Should().Contain("setCustomValidity");
    }

    [Fact]
    public void Los_horarios_del_catalogo_filtran_con_las_mismas_pestanas_de_topes()
    {
        var vista = File.ReadAllText(RutaVista());
        var js = File.ReadAllText(RutaSiteJs());
        var horarios = vista.IndexOf("id=\"catalogo-horarios-loterias\"", StringComparison.Ordinal);
        var titulo = vista.IndexOf("HorariosDeLoterias", horarios, StringComparison.Ordinal);
        var sub = vista.IndexOf("HorariosDeLoteriasSub", horarios, StringComparison.Ordinal);
        var tabs = vista.IndexOf("data-catalogo-jornadas", horarios, StringComparison.Ordinal);
        var buscar = vista.IndexOf("id=\"q\"", horarios, StringComparison.Ordinal);
        var jornadaCampo = vista.IndexOf("for=\"jornadaId\"", horarios, StringComparison.Ordinal);

        horarios.Should().BePositive();
        titulo.Should().BeGreaterThan(horarios);
        sub.Should().BeGreaterThan(titulo);
        tabs.Should().BeGreaterThan(sub);
        buscar.Should().BeGreaterThan(tabs);
        jornadaCampo.Should().Be(-1);
        var bloque = vista[horarios..];
        bloque.Should().Contain("UiTexts.Manana");
        bloque.Should().Contain("UiTexts.Tarde");
        bloque.Should().Contain("UiTexts.Noche");
        bloque.Should().Contain("UiTexts.TodasLasJornadas");
        bloque.Should().Contain("topes-jornada-tab");
        bloque.Should().NotContain("name=\"jornadaId\"");
        js.Should().Contain("data-catalogo-jornadas");
    }

    [Fact]
    public void El_catalogo_ordena_loteria_jornada_cierre_y_estado()
    {
        var vista = File.ReadAllText(RutaVista());
        var horarios = vista.IndexOf("id=\"catalogo-horarios-loterias\"", StringComparison.Ordinal);
        var encabezado = vista[horarios..vista.IndexOf("</thead>", horarios, StringComparison.Ordinal)];
        encabezado.Should().Contain("table-sort");
        encabezado.Should().Contain("UrlOrdenCatalogo(\"nombre\")");
        encabezado.Should().Contain("UrlOrdenCatalogo(\"jornada\")");
        encabezado.Should().Contain("UrlOrdenCatalogo(\"horaFin\")");
        encabezado.Should().Contain("UrlOrdenCatalogo(\"estado\")");
        encabezado.Should().Contain("UiTexts.HorarioHabilitado");
        encabezado.Should().NotContain("UrlOrdenCatalogo(\"horaInicio\")");
    }

    private static string RutaSiteJs()
    {
        var ruta = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Administrador", "wwwroot", "js", "site.js"));
        File.Exists(ruta).Should().BeTrue($"se esperaba el script en {ruta}");
        return ruta;
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
