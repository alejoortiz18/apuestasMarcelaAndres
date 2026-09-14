using NewRich.Domain.Enums;

namespace NewRich.Domain.Services;

/// <summary>Una jugada del boleto frente al número ganador de una lotería. Sin publicar cuando es null.</summary>
public sealed record LineaResultadoBoleto(string Loteria, string NumeroApostado, string? NumeroGanador)
{
    public bool Publicado => NumeroGanador is not null;

    public bool Acierta =>
        NumeroGanador is not null &&
        string.Equals(NumeroGanador.Trim(), NumeroApostado.Trim(), StringComparison.Ordinal);
}

/// <summary>
/// Un boleto gana apenas acierta en cualquier lotería publicada. Solo se declara no ganador
/// cuando todas sus loterías ya tienen número ganador; mientras falte alguna sigue jugado.
/// </summary>
public static class ResolucionBoletoRegla
{
    public static EstadoBoleto Resolver(IReadOnlyCollection<LineaResultadoBoleto> lineas)
    {
        if (lineas.Any(l => l.Acierta))
        {
            return EstadoBoleto.Ganador;
        }

        return lineas.Count > 0 && lineas.All(l => l.Publicado)
            ? EstadoBoleto.NoGanador
            : EstadoBoleto.Jugado;
    }

    public static bool FaltanResultados(IReadOnlyCollection<LineaResultadoBoleto> lineas) =>
        lineas.Any(l => !l.Publicado);
}
