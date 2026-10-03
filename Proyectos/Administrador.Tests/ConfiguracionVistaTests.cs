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
    public void El_catalogo_ordena_loteria_jornada_cierre_y_estado()
    {
        var vista = File.ReadAllText(RutaVista());
        var catalogo = vista.IndexOf("id=\"catalogo-loterias\"", StringComparison.Ordinal);
        var encabezado = vista[catalogo..vista.IndexOf("</thead>", catalogo, StringComparison.Ordinal)];

        catalogo.Should().BePositive();
        encabezado.Should().Contain("table-sort");
        encabezado.Should().Contain("UrlOrdenCatalogo(\"nombre\")");
        encabezado.Should().Contain("UrlOrdenCatalogo(\"jornada\")");
        encabezado.Should().Contain("UrlOrdenCatalogo(\"horaFin\")");
        encabezado.Should().Contain("UrlOrdenCatalogo(\"estado\")");
        encabezado.Should().Contain("MarcaOrdenCatalogo");
        encabezado.Should().Contain("UiTexts.HorarioHabilitado");
        vista[catalogo..].Should().Contain("item.JornadaNombre");
    }

    [Fact]
    public void El_catalogo_filtra_por_jornada_sin_perder_busqueda_ni_orden()
    {
        var vista = File.ReadAllText(RutaVista());
        var catalogo = vista.IndexOf("id=\"catalogo-loterias\"", StringComparison.Ordinal);
        var filtro = vista.IndexOf("data-filtro-jornada", catalogo, StringComparison.Ordinal);
        var buscar = vista.IndexOf("id=\"q\"", catalogo, StringComparison.Ordinal);
        var bloque = vista[filtro..buscar];

        filtro.Should().BeGreaterThan(catalogo);
        buscar.Should().BeGreaterThan(filtro);
        bloque.IndexOf("JornadaPorHoraCierre.Manana", StringComparison.Ordinal).Should().BePositive()
            .And.BeLessThan(bloque.IndexOf("JornadaPorHoraCierre.Tarde", StringComparison.Ordinal));
        bloque.IndexOf("JornadaPorHoraCierre.Noche", StringComparison.Ordinal)
            .Should().BeGreaterThan(bloque.IndexOf("JornadaPorHoraCierre.Tarde", StringComparison.Ordinal));
        bloque.Should().Contain("UiTexts.TodasLasJornadas");
        bloque.Should().Contain("aria-current");
        vista[catalogo..buscar].Should().Contain("name=\"jornada\"");
        vista[catalogo..buscar].Should().Contain("name=\"orden\"");
        vista[catalogo..buscar].Should().Contain("name=\"dir\"");
        var horarios = vista[vista.IndexOf("asp-action=\"GuardarHorarios\"", StringComparison.Ordinal)..];
        horarios.Should().Contain("name=\"jornada\"");
        horarios.Should().Contain("name=\"orden\"");
        horarios.Should().Contain("name=\"dir\"");
    }

    [Fact]
    public void Guardar_horarios_marca_los_cambios_y_solo_se_habilita_si_hay_alguno()
    {
        var vista = File.ReadAllText(RutaVista());
        var horarios = vista[vista.IndexOf("asp-action=\"GuardarHorarios\"", StringComparison.Ordinal)..];
        horarios.Should().Contain("data-horarios-catalogo");
        horarios.Should().Contain("data-horarios-guardados");
        horarios.Should().Contain("data-loteria-id=\"@item.LoteriaId\"");
        horarios.Should().Contain("data-hora=\"inicio\"");
        horarios.Should().Contain("data-hora=\"fin\"");
        horarios.Should().Contain("data-original=\"@item.HoraInicio\"");
        horarios.Should().Contain("data-original=\"@item.HoraFin\"");
        horarios.Should().Contain("data-horarios-extra");
        horarios.Should().Contain("data-horarios-pager");
        var boton = horarios[horarios.IndexOf("data-guardar-horarios", StringComparison.Ordinal)..];
        boton[..boton.IndexOf('>')].Should().Contain("disabled").And.Contain("UiTexts.GuardarHorariosDesactivado");

        var js = File.ReadAllText(RutaSiteJs());
        js.Should().Contain("nr-horarios-pendientes");
        js.Should().Contain("is-horario-cambio");
        js.Should().Contain("horarios[' + indice + '].LoteriaId");
        js.Should().Contain("getAttribute(\"data-horarios-guardados\") === \"1\"");
        File.ReadAllText(RutaSiteCss()).Should().Contain("tr.is-horario-cambio");
    }

    [Fact]
    public void Al_enviar_un_formulario_se_conserva_el_lugar_de_la_pantalla()
    {
        var js = File.ReadAllText(RutaSiteJs());
        js.Should().Contain("conservarLugarDeTrabajo");
        js.Should().Contain("\"nr.scroll.\" + location.pathname");
        js.Should().Contain("history.scrollRestoration");
        js.Should().Contain("pageshow");
    }

    [Fact]
    public void Topes_por_loteria_se_filtran_por_jornada_sin_dejar_de_enviar_todas()
    {
        var vista = File.ReadAllText(RutaVista());
        var inicio = vista.IndexOf("asp-action=\"GuardarTopes\"", StringComparison.Ordinal);
        inicio.Should().BePositive();
        var topes = vista[inicio..vista.IndexOf("</form>", inicio, StringComparison.Ordinal)];

        topes.Should().Contain("data-topes-jornadas");
        topes.Should().Contain("role=\"tablist\"");
        topes.Should().Contain("UiTexts.TopesPorJornada");
        topes.Should().Contain("JornadaPorHoraCierre.Manana");
        topes.Should().Contain("JornadaPorHoraCierre.Tarde");
        topes.Should().Contain("JornadaPorHoraCierre.Noche");
        topes.Should().Contain("UiTexts.TodasLasJornadas");
        topes.Should().Contain("JornadaPorHoraCierre.Nombre(");
        topes.Should().Contain("<tr data-jornada=");
        topes.Should().Contain("data-topes-vacio");
        topes.Should().Contain("name=\"topes[@i].LoteriaId\"");

        var js = File.ReadAllText(RutaSiteJs());
        js.Should().Contain("filtrarTopesPorJornada");
        js.Should().Contain("\"nr.topes.jornada\"");
        File.ReadAllText(RutaSiteCss()).Should().Contain("[data-topes-jornadas] [hidden]");
    }

    private static string RutaSiteJs() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "Administrador", "wwwroot", "js", "site.js"));

    private static string RutaSiteCss() => Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "Administrador", "wwwroot", "css", "site.css"));

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
