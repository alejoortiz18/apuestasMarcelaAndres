using System.Globalization;

namespace NewRich.Admin.Models;

public sealed record PuntoLineaRecaudo(DateOnly Fecha, decimal Debia, decimal Cobrado, decimal Generado = 0m)
{
    public decimal Pendiente => Math.Max(0m, Debia - Cobrado);

    public int Avance => Debia <= 0m
        ? 0
        : (int)Math.Min(100m, Math.Round(Cobrado * 100m / Debia, MidpointRounding.AwayFromZero));
}

/// <param name="Nombre">Recaudador, grupo o alcance que se nombra en el resumen accesible del gráfico.</param>
public sealed record GraficoLineaRecaudoVista(GraficoLineaRecaudo Grafico, string Nombre);

/// <summary>Coordenadas del gráfico de línea de tiempo del detalle de recaudo, en unidades del viewBox del SVG.</summary>
public sealed class GraficoLineaRecaudo
{
    public const double Ancho = 760;
    public const double Alto = 280;
    public const double MargenIzquierdo = 92;
    public const double MargenDerecho = 20;
    public const double MargenSuperior = 16;
    public const double MargenInferior = 36;
    public const int Divisiones = 4;
    private const int FechasVisibles = 10;

    private const double AnchoBarraMaximo = 26;

    private GraficoLineaRecaudo(IReadOnlyList<PuntoLineaRecaudo> puntos, decimal maximo, bool mostrarGenerado)
    {
        Puntos = puntos;
        Maximo = maximo;
        MostrarGenerado = mostrarGenerado;
    }

    public IReadOnlyList<PuntoLineaRecaudo> Puntos { get; }
    public decimal Maximo { get; }

    /// <summary>Dibuja una barra dorada por día con lo generado por las ventas.</summary>
    public bool MostrarGenerado { get; }
    public bool SinMovimiento => Puntos.All(p => p.Debia <= 0m && p.Cobrado <= 0m && p.Generado <= 0m);
    public double Base => Alto - MargenInferior;

    public static GraficoLineaRecaudo De(IReadOnlyList<PuntoLineaRecaudo> puntos, bool mostrarGenerado = false)
    {
        var mayor = puntos.Count == 0 ? 0m : puntos.Max(p => Math.Max(Math.Max(p.Debia, p.Cobrado), mostrarGenerado ? p.Generado : 0m));
        return new GraficoLineaRecaudo(puntos, Redondear(mayor), mostrarGenerado);
    }

    public double AnchoBarra =>
        Math.Min(AnchoBarraMaximo, (Ancho - MargenIzquierdo - MargenDerecho) / Math.Max(1, Puntos.Count) * 0.5);

    public double BarraX(int indice) => X(indice) - AnchoBarra / 2;

    public double AltoBarra(decimal valor) => Base - Y(valor);

    public double X(int indice)
    {
        var util = Ancho - MargenIzquierdo - MargenDerecho;
        return Puntos.Count <= 1
            ? MargenIzquierdo + util / 2
            : MargenIzquierdo + indice * util / (Puntos.Count - 1);
    }

    public double Y(decimal valor) =>
        MargenSuperior + (Base - MargenSuperior) * (1 - (double)(Math.Clamp(valor, 0m, Maximo) / Maximo));

    public string Trazo(Func<PuntoLineaRecaudo, decimal> valor) =>
        string.Join(' ', Puntos.Select((p, i) => $"{Numero(X(i))},{Numero(Y(valor(p)))}"));

    public IEnumerable<decimal> Escala() =>
        Enumerable.Range(0, Divisiones + 1).Select(i => Maximo * i / Divisiones);

    public bool MostrarFecha(int indice)
    {
        var paso = (int)Math.Ceiling(Puntos.Count / (double)FechasVisibles);
        return indice == Puntos.Count - 1 || (indice % paso == 0 && Puntos.Count - 1 - indice >= paso / 2.0);
    }

    /// <summary>Centro de la franja del día, en porcentaje del área de trazado (0 a 100).</summary>
    public double PosicionX(int indice) =>
        (X(indice) - MargenIzquierdo) * 100 / (Ancho - MargenIzquierdo - MargenDerecho);

    /// <summary>Altura del valor en porcentaje del área de trazado: 0 arriba, 100 en la base.</summary>
    public double PosicionY(decimal valor) => (Y(valor) - MargenSuperior) * 100 / (Base - MargenSuperior);

    public double AnchoFranja => 100.0 / Math.Max(1, Puntos.Count);

    public bool TooltipALaIzquierda(int indice) => PosicionX(indice) > 60;

    public static string FechaLarga(DateOnly fecha)
    {
        var texto = fecha.ToString("dddd d 'de' MMMM 'de' yyyy", Cultura);
        return char.ToUpper(texto[0], Cultura) + texto[1..];
    }

    public static string Numero(double valor) => valor.ToString("0.##", CultureInfo.InvariantCulture);

    private static readonly CultureInfo Cultura = new("es-CO");

    private static decimal Redondear(decimal mayor)
    {
        if (mayor <= 0m)
        {
            return 1m;
        }

        var magnitud = (decimal)Math.Pow(10, Math.Floor(Math.Log10((double)mayor)));
        foreach (var factor in new[] { 1m, 2m, 2.5m, 5m, 10m })
        {
            if (magnitud * factor >= mayor)
            {
                return magnitud * factor;
            }
        }

        return magnitud * 10m;
    }
}
