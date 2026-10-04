using System.Globalization;
using NewRich.Application.Contracts.Recaudo;
using NewRich.Domain.Services;

namespace NewRich.Pda.Core;

public readonly record struct ResumenTurnoRecaudo(decimal TotalPorRecaudar, decimal TotalRecaudado, int Grupos, int Vendedores);

/// <summary>Botones de lista de la pantalla Recaudar.</summary>
public enum FiltroCobro
{
    /// <summary>Ventas de hoy que todavía no pasan por un cobro.</summary>
    Hoy,

    /// <summary>Vendedores a los que se les cobró hoy.</summary>
    Cobrados,

    /// <summary>Vendedores con deuda de días anteriores o que quedó tras un cobro.</summary>
    Adeudados
}

public static class RecaudoListas
{
    public const string SinGrupo = "Sin grupo";

    public static ResumenTurnoRecaudo Resumen(IEnumerable<ObligacionRecaudoResponse> filas)
    {
        var lista = filas as IReadOnlyCollection<ObligacionRecaudoResponse> ?? filas.ToList();
        return new ResumenTurnoRecaudo(
            lista.Sum(f => f.ValorACobrar),
            lista.Sum(f => f.PagosHoy),
            Grupos(lista).Count,
            lista.Count);
    }

    public static string FechaDelDia(DateTime utc) =>
        ZonaHorariaColombia.ALocal(utc).ToString("d 'de' MMMM 'de' yyyy", CultureInfo.GetCultureInfo("es-CO"));

    public static IReadOnlyList<string> Grupos(IEnumerable<ObligacionRecaudoResponse> filas) =>
        filas
            .Select(f => f.Grupo)
            .Where(g => !string.IsNullOrWhiteSpace(g) && !string.Equals(g, SinGrupo, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static ObligacionRecaudoResponse TrasCobro(ObligacionRecaudoResponse fila, decimal valor)
    {
        fila.PagosHoy += valor;
        fila.TotalPendiente = Math.Max(0m, fila.TotalPendiente - valor);
        fila.PendienteDelDia = 0m;
        fila.Lista = nameof(ListaCobro.Cobrados);
        if (fila.TotalPendiente <= 0m)
        {
            fila.Estado = nameof(EstadoCobro.AlDia);
            fila.Color = nameof(ColorCobro.Azul);
        }
        else
        {
            fila.Estado = nameof(EstadoCobro.Deudado);
            fila.Color = nameof(ColorCobro.Rojo);
        }

        return fila;
    }

    public static DateOnly HoyEnColombia() => DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.UtcNow));

    /// <summary>
    /// Los cobros guardados sin conexión que el API todavía no tiene se aplican sobre la lista descargada.
    /// Los de otro día solo bajan la deuda: no cuentan como cobro de hoy.
    /// </summary>
    public static IReadOnlyList<ObligacionRecaudoResponse> ConPendientes(
        IReadOnlyList<ObligacionRecaudoResponse> filas,
        IEnumerable<PagoPendienteRecaudo> pendientes,
        DateOnly hoy)
    {
        foreach (var pago in pendientes)
        {
            var fila = filas.FirstOrDefault(f => f.VendedorId == pago.VendedorId);
            if (fila is null)
            {
                continue;
            }

            if (DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(DateTime.SpecifyKind(pago.FechaLocal, DateTimeKind.Utc))) == hoy)
            {
                TrasCobro(fila, pago.Valor);
            }
            else
            {
                fila.TotalPendiente = Math.Max(0m, fila.TotalPendiente - pago.Valor);
            }
        }

        return filas;
    }

    /// <summary>
    /// La lista guardada en el PDA solo vale para el día en que se descargó. De otro día (o sin fecha) queda
    /// solo la deuda: lo vendido, lo generado y lo cobrado ese día vuelven a cero.
    /// </summary>
    public static IReadOnlyList<ObligacionRecaudoResponse> DelDia(
        IReadOnlyList<ObligacionRecaudoResponse> filas,
        string? diaGuardado,
        DateOnly hoy)
    {
        if (DateOnly.TryParseExact(diaGuardado, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia) && dia == hoy)
        {
            return filas;
        }

        var quedan = new List<ObligacionRecaudoResponse>();
        foreach (var fila in filas)
        {
            var deuda = Math.Max(0m, fila.TotalPendiente);
            if (EstadoCobroRecaudoRegla.Clasificar(deuda, 0m, 0m) is not { } clasificacion)
            {
                continue;
            }

            fila.TotalVendido = 0m;
            fila.ValorACobrar = 0m;
            fila.PagosHoy = 0m;
            fila.PendienteDelDia = 0m;
            fila.UltimoPago = null;
            fila.SaldoAnterior = deuda;
            fila.TotalPendiente = deuda;
            fila.Estado = clasificacion.Estado.ToString();
            fila.Color = clasificacion.Color.ToString();
            fila.Lista = clasificacion.Lista.ToString();
            quedan.Add(fila);
        }

        return quedan;
    }

    public static bool Cumple(ObligacionRecaudoResponse fila, FiltroCobro filtro) => filtro switch
    {
        FiltroCobro.Hoy => RecaudoPagoVista.Saldos(fila).PendienteDelDia > 0m,
        FiltroCobro.Cobrados => fila.PagosHoy > 0m
            && string.Equals(fila.Lista, nameof(ListaCobro.Cobrados), StringComparison.OrdinalIgnoreCase),
        FiltroCobro.Adeudados => RecaudoPagoVista.Saldos(fila).SaldoAnterior > 0m,
        _ => true
    };

    public static IReadOnlyList<ObligacionRecaudoResponse> De(
        IEnumerable<ObligacionRecaudoResponse> filas,
        FiltroCobro filtro,
        string busqueda,
        string orden,
        string? grupo = null) =>
        De(filas.Where(f => Cumple(f, filtro)), (ListaCobro?)null, busqueda, orden, grupo);

    public static IReadOnlyList<ObligacionRecaudoResponse> De(
        IEnumerable<ObligacionRecaudoResponse> filas,
        ListaCobro? lista,
        string busqueda,
        string orden,
        string? grupo = null)
    {
        var texto = (busqueda ?? string.Empty).Trim();
        var filtradas = filas.Where(f =>
        {
            if (lista is ListaCobro destino &&
                !string.Equals(f.Lista, destino.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(grupo) &&
                !string.Equals(f.Grupo, grupo, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(texto))
            {
                return true;
            }

            return Contiene(f.NombreCompleto, texto)
                || Contiene(f.Alias, texto)
                || Contiene(f.Usuario, texto)
                || Contiene(f.Documento, texto);
        });

        filtradas = orden switch
        {
            "vendido" => filtradas.OrderByDescending(f => f.TotalVendido).ThenBy(f => f.NombreCompleto),
            "cobrar" => filtradas.OrderByDescending(f => f.ValorACobrar).ThenBy(f => f.NombreCompleto),
            "pendiente" => filtradas.OrderByDescending(f => f.TotalPendiente).ThenBy(f => f.NombreCompleto),
            "recibido" => filtradas.OrderByDescending(f => f.PagosHoy).ThenBy(f => f.NombreCompleto),
            "estado" => filtradas.OrderBy(f => f.Estado).ThenBy(f => f.NombreCompleto),
            _ => filtradas.OrderBy(f => f.NombreCompleto)
        };

        return filtradas.ToList();
    }

    public static IReadOnlyList<IGrouping<string, ObligacionRecaudoResponse>> Agrupar(
        IEnumerable<ObligacionRecaudoResponse> filas)
    {
        return filas
            .GroupBy(f => string.IsNullOrWhiteSpace(f.Grupo) || f.Grupo == SinGrupo ? SinGrupo : f.Grupo)
            .OrderBy(g => g.Key == SinGrupo)
            .ThenBy(g => g.Key)
            .ToList();
    }

    private static bool Contiene(string? valor, string texto) =>
        !string.IsNullOrEmpty(valor) && valor.Contains(texto, StringComparison.OrdinalIgnoreCase);
}
