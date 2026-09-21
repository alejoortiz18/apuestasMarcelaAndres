using System.Globalization;
using NewRich.Admin.Constants;
using NewRich.Admin.Models;
using NewRich.Application.Contracts.Kpi;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace NewRich.Admin.Services;

public static class KpiPdf
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");
    private static readonly Color Tinta = Color.FromHex("#1c2418");
    private static readonly Color Apagado = Color.FromHex("#7a7464");
    private static readonly Color Papel = Color.FromHex("#f4efe4");
    private static readonly Color Panel = Color.FromHex("#fffdf8");
    private static readonly Color Linea = Color.FromHex("#e6d9b8");
    private static readonly Color Verde = Color.FromHex("#173b32");
    private static readonly Color Oro = Color.FromHex("#c9a44a");
    private static readonly Color Azul = Color.FromHex("#246b9f");
    private static readonly Color Alerta = Color.FromHex("#bc4747");

    public static byte[] Crear(KpiIndexViewModel vista)
    {
        var kpi = vista.Kpi ?? throw new ArgumentException(UiTexts.Vacio);
        QuestPDF.Settings.License = LicenseType.Community;
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(22);
                page.PageColor(Papel);
                page.DefaultTextStyle(t => t.FontSize(9).FontColor(Tinta));
                page.Header().Element(h => Encabezado(h, kpi));
                page.Footer().Element(Pie);
                page.Content().Column(col =>
                {
                    col.Spacing(12);
                    col.Item().Element(c => Filtros(c, vista, kpi));
                    col.Item().Element(c => IngresosYVentas(c, kpi));
                    col.Item().Element(c => Personal(c, kpi));
                    col.Item().Element(c => Premios(c, kpi));
                    col.Item().Element(c => Salud(c, kpi));
                    col.Item().Element(c => Alertas(c, kpi));
                    col.Item().Element(c => VentasDia(c, kpi));
                    col.Item().Element(c => Resultados(c, kpi));
                });
            });
        }).GeneratePdf();
    }

    private static void Encabezado(IContainer container, KpiResponse kpi)
    {
        container.Column(col =>
        {
            col.Item().Text(UiTexts.KpiKicker).FontSize(9).FontColor(Oro);
            col.Item().Text(UiTexts.KpiTitulo).FontSize(16).Bold().FontColor(Verde);
            col.Item().Text(UiTexts.KpiSub).FontSize(8).FontColor(Apagado);
            col.Item().PaddingTop(4).Text($"{UiTexts.CorteDeInformacion}: {kpi.Corte:dd/MM/yyyy HH:mm} · {UiTexts.FuenteSql}").FontSize(8).FontColor(Apagado);
            col.Item().PaddingBottom(6).LineHorizontal(1).LineColor(Oro);
        });
    }

    private static void Pie(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().AlignLeft().Text(UiTexts.KpiTitulo).FontSize(8).FontColor(Apagado);
            row.RelativeItem().AlignRight().Text(texto =>
            {
                texto.DefaultTextStyle(t => t.FontSize(8).FontColor(Apagado));
                texto.CurrentPageNumber();
                texto.Span(" / ");
                texto.TotalPages();
            });
        });
    }

    private static void Filtros(IContainer container, KpiIndexViewModel vista, KpiResponse kpi)
    {
        var grupo = vista.Grupos.FirstOrDefault(g => g.GrupoId == vista.GrupoId)?.Nombre ?? UiTexts.General;
        var vendedor = vista.Vendedores.FirstOrDefault(v => v.UsuarioId == vista.VendedorId)?.NombreCompleto ?? UiTexts.Todos;
        var periodo = vista.Periodo switch
        {
            "7" => UiTexts.Ultimos7Dias,
            "mes" => UiTexts.EsteMes,
            "personalizado" => UiTexts.PeriodoPersonalizado,
            _ => UiTexts.Ultimos30Dias
        };
        container.Background(Panel).Border(1).BorderColor(Linea).Padding(10).Column(col =>
        {
            col.Spacing(4);
            col.Item().Text(UiTexts.FiltrosAplicados).Bold().FontColor(Verde);
            col.Item().Text($"{UiTexts.Periodo}: {periodo}");
            col.Item().Text($"{UiTexts.Grupo}: {grupo}");
            col.Item().Text($"{UiTexts.Vendedor}: {vendedor}");
            col.Item().Text($"{UiTexts.CompararCon}: {(vista.CompararAnterior ? UiTexts.PeriodoAnterior : UiTexts.SinComparacion)}");
            col.Item().Text($"{UiTexts.FechaInicial}: {vista.FechaInicial:dd/MM/yyyy} · {UiTexts.FechaFinal}: {vista.FechaFinal:dd/MM/yyyy}");
            col.Item().Text(kpi.FiltroAplicado).FontColor(Apagado);
        });
    }

    private static void IngresosYVentas(IContainer container, KpiResponse kpi)
    {
        container.Column(col =>
        {
            col.Spacing(8);
            col.Item().Element(c => PanelContenido(c, inner =>
            {
                inner.Item().Text(UiTexts.IngresosYVentas).Bold().FontSize(12).FontColor(Verde);
                inner.Item().Text(UiTexts.ResultadoFinancieroSub).FontSize(8).FontColor(Apagado);
                inner.Item().Element(m => Metricas(m,
                [
                    Metrica(UiTexts.IngresosRegistrados, Dinero(kpi.Ingresos), Variacion(kpi.VariacionIngresos), kpi.VariacionIngresos < 0),
                    Metrica(UiTexts.VentasConfirmadas, kpi.VentasConfirmadas.ToString("N0", Cultura), string.Format(UiTexts.VentasPorDiaEtiqueta, kpi.VentasPorDia.ToString("N1", Cultura))),
                    Metrica(UiTexts.NumeroMasJugado, kpi.NumeroMasJugado ?? "—", UiTexts.SinComparacion),
                    Metrica(UiTexts.ValorMasAltoApostado, Dinero(kpi.ValorMasAltoApostado), UiTexts.SinComparacion),
                    Metrica(UiTexts.BoletosGanadores, kpi.BoletosGanadores.ToString("N0", Cultura), string.Format(UiTexts.DelTotal, kpi.PorcentajeGanadores.ToString("N1", Cultura)))
                ]));
                inner.Item().Element(b => BarrasGrupo(b, kpi));
            }));
            col.Item().Element(c => TablaVendedores(c, kpi));
        });
    }

    private static void BarrasGrupo(IContainer container, KpiResponse kpi)
    {
        container.Column(col =>
        {
            col.Spacing(6);
            col.Item().Text(UiTexts.IngresosPorGrupo).Bold().FontColor(Verde);
            if (kpi.IngresosPorGrupo.Count == 0)
            {
                col.Item().Text(UiTexts.Vacio).FontColor(Apagado);
                return;
            }

            var i = 0;
            foreach (var barra in kpi.IngresosPorGrupo)
            {
                var color = i % 3 == 1 ? Azul : i % 3 == 2 ? Oro : Verde;
                var ancho = Math.Clamp(barra.Ancho, 0, 100);
                col.Item().Column(b =>
                {
                    b.Item().Row(r =>
                    {
                        r.RelativeItem().Text($"{barra.Nombre} · {barra.Porcentaje.ToString("N0", Cultura)}%").FontSize(8);
                        r.AutoItem().Text(Dinero(barra.Total)).Bold().FontSize(8);
                    });
                    b.Item().Height(8).Row(r =>
                    {
                        if (ancho > 0)
                        {
                            r.RelativeItem((float)ancho).Background(color);
                        }

                        if (ancho < 100)
                        {
                            r.RelativeItem((float)(100 - ancho)).Background(Color.FromHex("#f4ead0"));
                        }
                    });
                });
                i++;
            }
        });
    }

    private static void TablaVendedores(IContainer container, KpiResponse kpi)
    {
        container.Column(col =>
        {
            col.Spacing(6);
            col.Item().Text(UiTexts.IngresosPorPersona).Bold().FontColor(Verde);
            col.Item().Element(c => Tabla(c,
                [UiTexts.Vendedor, UiTexts.Grupo, UiTexts.Ventas, UiTexts.Total],
                kpi.IngresosPorVendedor.Select(f => new[]
                {
                    f.Vendedor,
                    f.Grupo,
                    f.Ventas.ToString("N0", Cultura),
                    Dinero(f.Total)
                }),
                [2f, 2f, 1f, 1f]));
        });
    }

    private static void Personal(IContainer container, KpiResponse kpi)
    {
        container.Element(c => PanelContenido(c, col =>
        {
            col.Item().Text(UiTexts.Personal).Bold().FontSize(12).FontColor(Verde);
            col.Item().Text(UiTexts.DimensionPersonasSub).FontSize(8).FontColor(Apagado);
            col.Item().Element(c => Metricas(c,
            [
                Metrica(UiTexts.PersonasTotales, kpi.PersonasTotales.ToString(Cultura), string.Format(UiTexts.VendedoresEtiqueta, kpi.Vendedores)),
                Metrica(UiTexts.Observador, kpi.Observadores.ToString(Cultura), string.Format(UiTexts.ObservadoresHoy, kpi.ObservadoresActivos)),
                Metrica(UiTexts.Administradores, kpi.Administradores.ToString(Cultura), string.Format(UiTexts.ConSesion, kpi.AdministradoresConSesion)),
                Metrica(UiTexts.UsuariosActivos, kpi.UsuariosActivos.ToString(Cultura), string.Format(UiTexts.CoberturaUsuarios, kpi.CoberturaActivos.ToString("N1", Cultura)))
            ]));
            col.Item().Text(string.Format(UiTexts.GruposActivosNota, kpi.GruposConVendedores, kpi.VendedoresSinGrupo, kpi.PdasAsignados)).FontSize(8).FontColor(Apagado);
        }));
    }

    private static void Premios(IContainer container, KpiResponse kpi)
    {
        var totalCasos = Math.Max(kpi.CasosAbiertos + kpi.CasosCerrados + kpi.CasosRechazados, 1);
        container.Element(c => PanelContenido(c, col =>
        {
            col.Item().Text(UiTexts.PremiosYEntregas).Bold().FontSize(12).FontColor(Verde);
            col.Item().Text(UiTexts.PremiosYEntregasSub).FontSize(8).FontColor(Apagado);
            col.Item().Element(c => Metricas(c,
            [
                Metrica(UiTexts.ValorEntregado, Dinero(kpi.ValorPremiosEntregados), string.Format(UiTexts.EntregasCantidad, kpi.EntregasPremio)),
                Metrica(UiTexts.CasosAbiertos, kpi.CasosAbiertos.ToString("00"), string.Format(UiTexts.PorValidar, kpi.CasosReportados)),
                Metrica(UiTexts.CasosCerrados, kpi.CasosCerrados.ToString(Cultura), string.Format(UiTexts.DelTotalCasos, kpi.PorcentajeCasosCerrados.ToString("N1", Cultura))),
                Metrica(UiTexts.Rechazados, kpi.CasosRechazados.ToString("00"), UiTexts.RequierenSeguimiento, kpi.CasosRechazados > 0)
            ]));
            col.Item().Element(b => BarraSimple(b, UiTexts.Reportados, kpi.CasosReportados.ToString(Cultura), kpi.CasosReportados * 100m / totalCasos, Oro));
            col.Item().Element(b => BarraSimple(b, UiTexts.ValidadosYAsignados, kpi.CasosValidadosYAsignados.ToString(Cultura), kpi.CasosValidadosYAsignados * 100m / totalCasos, Azul));
        }));
    }

    private static void Salud(IContainer container, KpiResponse kpi)
    {
        container.Element(c => PanelContenido(c, col =>
        {
            col.Item().Text(UiTexts.SaludOperativa).Bold().FontSize(12).FontColor(Verde);
            col.Item().Text(UiTexts.SaludOperativaSub).FontSize(8).FontColor(Apagado);
            col.Item().Element(c => Metricas(c,
            [
                Metrica(UiTexts.PdasConectados, $"{kpi.PdasConectados} / {kpi.PdasTotales}", string.Format(UiTexts.DisponiblesPda, kpi.PorcentajePdasConectados.ToString("N1", Cultura))),
                Metrica(UiTexts.ConversacionesAbiertas, kpi.ConversacionesAbiertas.ToString(Cultura), UiTexts.EnSoporte),
                Metrica(UiTexts.NumerosJugadosKpi, kpi.NumerosJugados.ToString(Cultura), string.Format(UiTexts.DelTotalBoletos, kpi.Boletos.ToString("N0", Cultura))),
                Metrica(UiTexts.AlertasActivas, kpi.AlertasActivas.ToString("00"), string.Format(UiTexts.AlertasDePremios, kpi.CasosReportados), kpi.AlertasActivas > 0)
            ]));
            if (kpi.LoteriasUtilizadas.Count > 0)
            {
                col.Item().Text($"{UiTexts.LoteriasUtilizadas}: {string.Join(", ", kpi.LoteriasUtilizadas)}").FontSize(8).FontColor(Apagado);
            }
        }));
    }

    private static void Alertas(IContainer container, KpiResponse kpi)
    {
        container.Column(col =>
        {
            col.Spacing(8);
            col.Item().Element(c => PanelContenido(c, inner =>
            {
                inner.Item().Text(UiTexts.RiesgosYAlertas).Bold().FontSize(12).FontColor(Verde);
                inner.Item().Text(UiTexts.RiesgosYAlertasSub).FontSize(8).FontColor(Apagado);
            }));
            col.Item().Element(c => Tabla(c,
                [UiTexts.Prioridad, UiTexts.Evento, UiTexts.Impacto, UiTexts.Accion],
                kpi.Alertas.Select(a => new[] { a.Prioridad, a.Evento, a.Impacto, UiTexts.Revisar }),
                [1f, 3f, 1f, 1f]));
        });
    }

    private static void VentasDia(IContainer container, KpiResponse kpi)
    {
        container.Column(col =>
        {
            col.Spacing(8);
            col.Item().Element(c => PanelContenido(c, inner =>
            {
                inner.Item().Text(UiTexts.VentasPorDia).Bold().FontSize(12).FontColor(Verde);
            }));
            col.Item().Element(c => Tabla(c,
                [UiTexts.Fecha, UiTexts.Ventas, UiTexts.Total],
                kpi.VentasPorDiaDetalle.Select(d => new[]
                {
                    d.Fecha.ToString("dd/MM/yyyy"),
                    d.Ventas.ToString("N0", Cultura),
                    Dinero(d.Total)
                }),
                [2f, 1f, 1f]));
        });
    }

    private static void Resultados(IContainer container, KpiResponse kpi)
    {
        container.Column(col =>
        {
            col.Spacing(8);
            col.Item().Element(c => PanelContenido(c, inner =>
            {
                inner.Item().Text(UiTexts.NumerosGanadoresKpi).Bold().FontSize(12).FontColor(Verde);
            }));
            col.Item().Element(c => Tabla(c,
                [UiTexts.Fecha, UiTexts.Loteria, UiTexts.NumeroGanador],
                kpi.Resultados.Select(r => new[]
                {
                    r.Fecha.ToString("dd/MM/yyyy"),
                    r.Loteria,
                    r.Numero
                }),
                [2f, 2f, 1f]));
        });
    }

    private static void PanelContenido(IContainer container, Action<ColumnDescriptor> contenido)
    {
        container.Background(Panel).Border(1).BorderColor(Linea).Padding(10).Column(col =>
        {
            col.Spacing(8);
            contenido(col);
        });
    }

    private static void Metricas(IContainer container, IReadOnlyList<(string Etiqueta, string Valor, string Detalle, bool Alerta)> metricas)
    {
        container.Column(col =>
        {
            col.Spacing(6);
            for (var i = 0; i < metricas.Count; i += 2)
            {
                var izquierda = metricas[i];
                var hayDerecha = i + 1 < metricas.Count;
                col.Item().Row(row =>
                {
                    row.RelativeItem().Element(c => Tarjeta(c, izquierda));
                    row.ConstantItem(6);
                    if (hayDerecha)
                    {
                        row.RelativeItem().Element(c => Tarjeta(c, metricas[i + 1]));
                    }
                    else
                    {
                        row.RelativeItem();
                    }
                });
            }
        });
    }

    private static void Tarjeta(IContainer container, (string Etiqueta, string Valor, string Detalle, bool Alerta) metrica)
    {
        container.Border(1).BorderColor(Linea).Background(Papel).Padding(8).Column(col =>
        {
            col.Item().Text(metrica.Etiqueta).FontSize(8).FontColor(Apagado);
            col.Item().Text(metrica.Valor).Bold().FontSize(12).FontColor(metrica.Alerta ? Alerta : Verde);
            col.Item().Text(metrica.Detalle).FontSize(8).FontColor(Apagado);
        });
    }

    private static void BarraSimple(IContainer container, string etiqueta, string valor, decimal porcentaje, Color color)
    {
        var ancho = Math.Clamp(Math.Round(porcentaje), 0, 100);
        container.Column(col =>
        {
            col.Item().Row(r =>
            {
                r.RelativeItem().Text(etiqueta).FontSize(8);
                r.AutoItem().Text(valor).Bold().FontSize(8);
            });
            col.Item().Height(8).Row(r =>
            {
                if (ancho > 0)
                {
                    r.RelativeItem((float)ancho).Background(color);
                }

                if (ancho < 100)
                {
                    r.RelativeItem((float)(100 - ancho)).Background(Color.FromHex("#f4ead0"));
                }
            });
        });
    }

    private static void Tabla(IContainer container, IReadOnlyList<string> encabezados, IEnumerable<string[]> filas, IReadOnlyList<float>? pesos = null)
    {
        var lista = filas.ToList();
        if (lista.Count == 0)
        {
            container.Text(UiTexts.Vacio).FontColor(Apagado);
            return;
        }

        container.Table(t =>
        {
            t.ColumnsDefinition(c =>
            {
                for (var i = 0; i < encabezados.Count; i++)
                {
                    c.RelativeColumn(pesos is null ? 1 : pesos[i]);
                }
            });
            t.Header(h =>
            {
                foreach (var titulo in encabezados)
                {
                    h.Cell().Background(Verde).Padding(5).Text(titulo).FontColor(Color.FromHex("#f0d078")).FontSize(8).Bold();
                }
            });
            var i = 0;
            foreach (var fila in lista)
            {
                var fondo = i % 2 == 0 ? Panel : Papel;
                foreach (var celda in fila)
                {
                    t.Cell().Background(fondo).BorderBottom(1).BorderColor(Linea).Padding(5).Text(celda).FontSize(8);
                }

                i++;
            }
        });
    }

    private static (string Etiqueta, string Valor, string Detalle, bool Alerta) Metrica(string etiqueta, string valor, string detalle, bool alerta = false) =>
        (etiqueta, valor, detalle, alerta);

    private static string Dinero(decimal valor) => valor.ToString("C0", Cultura);

    private static string Variacion(decimal? valor)
    {
        if (!valor.HasValue)
        {
            return UiTexts.SinComparacion;
        }

        var signo = valor.Value >= 0 ? "+" : string.Empty;
        return $"{signo}{valor.Value.ToString("N1", Cultura)}% {UiTexts.VsPeriodoAnterior}";
    }
}
