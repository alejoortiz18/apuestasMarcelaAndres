using NewRich.Application.Contracts.Boletos;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Application.Contracts.Ventas;
using NewRich.Constants.Messages;
using NewRich.Domain.Enums;

namespace NewRich.Pda.Core;

/// <summary>Total vendido por un vendedor dentro del rango consultado.</summary>
public sealed record TotalVendedor(Guid VendedorId, string Vendedor, string Grupo, decimal Total, int Ventas);

/// <summary>Presentación de los resultados de las consultas del observador.</summary>
public static class ConsultaObservadorResultados
{
    /// <summary>Verde con el que se resalta un boleto o venta ganadora.</summary>
    public const string ColorGanador = "#a3f1ad";

    /// <summary>Lila con el que se resalta un premio ya entregado.</summary>
    public const string ColorEntregado = "#cfd0e3";

    /// <summary>Un ganador sigue siendo seleccionable aunque el premio ya se haya pagado o entregado.</summary>
    public static bool EsGanador(string? estadoVisual) =>
        estadoVisual is not null
        && (Igual(estadoVisual, BoletoMessages.BoletoGanador)
            || Igual(estadoVisual, BoletoMessages.BoletoPagado)
            || Igual(estadoVisual, BoletoMessages.BoletoPremioEntregado)
            || Igual(estadoVisual, nameof(EstadoBoleto.Ganador))
            || Igual(estadoVisual, nameof(EstadoBoleto.PagadoCobrado))
            || Igual(estadoVisual, nameof(EstadoBoleto.PremioEntregado)));

    public static bool EsPremioEntregado(string? estadoVisual) =>
        estadoVisual is not null
        && (Igual(estadoVisual, BoletoMessages.BoletoPremioEntregado)
            || Igual(estadoVisual, nameof(EstadoBoleto.PremioEntregado)));

    /// <summary>
    /// Color de fondo de la tarjeta: lila si el premio ya se entregó, verde si ganó, nulo en el resto.
    /// </summary>
    public static string? ColorResaltado(string? estadoVisual)
    {
        if (EsPremioEntregado(estadoVisual))
        {
            return ColorEntregado;
        }

        return EsGanador(estadoVisual) ? ColorGanador : null;
    }

    public static BoletoListaResponse ComoBoleto(VentaResponse venta) => new()
    {
        BoletoId = venta.BoletoId,
        CodigoPublico = venta.CodigoPublico,
        Vendedor = venta.Vendedor,
        Fecha = venta.FechaVenta,
        Total = venta.Total,
        Estado = venta.EstadoBoleto
    };

    public static string TipoApuestaTexto(TipoApuesta tipo) =>
        tipo == TipoApuesta.COMBINADO ? PdaTexts.TipoCombinada : PdaTexts.TipoIndividual;

    public static string NumerosApostados(VentaResponse venta) =>
        string.Join(", ", venta.Juegos
            .Select(j => j.Numero)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase));

    public static IReadOnlyList<TotalVendedor> TotalesPorVendedor(
        IEnumerable<UsuarioResponse> usuarios,
        IEnumerable<VentaResponse> ventas)
    {
        var porVendedor = ventas
            .GroupBy(v => v.VendedorId)
            .ToDictionary(g => g.Key, g => (Total: g.Sum(v => v.Total), Ventas: g.Count()));

        return usuarios
            .Where(u => u.Rol == RolUsuario.Vendedor)
            .Select(u =>
            {
                porVendedor.TryGetValue(u.UsuarioId, out var acumulado);
                return new TotalVendedor(
                    u.UsuarioId,
                    string.IsNullOrWhiteSpace(u.NombreCompleto) ? u.Usuario : u.NombreCompleto,
                    u.GrupoNombre ?? PdaTexts.GrupoNoConsultado,
                    acumulado.Total,
                    acumulado.Ventas);
            })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Vendedor, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static bool Igual(string valor, string esperado) =>
        string.Equals(valor.Trim(), esperado, StringComparison.OrdinalIgnoreCase);
}
