namespace NewRich.Domain.Services;

/// <summary>Mes calendario usado por la retención. El corte es la medianoche de Colombia.</summary>
public readonly record struct MesCalendario(int Anio, int Mes) : IComparable<MesCalendario>
{
    public MesCalendario(DateOnly fecha)
        : this(fecha.Year, fecha.Month)
    {
    }

    public DateOnly PrimerDia => new(Anio, Mes, 1);

    public MesCalendario Agregar(int meses)
    {
        var fecha = PrimerDia.AddMonths(meses);
        return new MesCalendario(fecha.Year, fecha.Month);
    }

    public int AntiguedadInclusive(MesCalendario referencia)
    {
        var diferencia = ((referencia.Anio - Anio) * 12) + (referencia.Mes - Mes);
        return diferencia + 1;
    }

    public static MesCalendario DeInstanteUtc(DateTime instanteUtc) =>
        new(DateOnly.FromDateTime(ZonaHorariaColombia.ALocal(instanteUtc)));

    public static MesCalendario Parse(string valor)
    {
        var partes = valor.Split('-');
        return new MesCalendario(int.Parse(partes[0]), int.Parse(partes[1]));
    }

    public int CompareTo(MesCalendario other)
    {
        var anio = Anio.CompareTo(other.Anio);
        return anio != 0 ? anio : Mes.CompareTo(other.Mes);
    }

    public static bool operator <(MesCalendario left, MesCalendario right) => left.CompareTo(right) < 0;

    public static bool operator >(MesCalendario left, MesCalendario right) => left.CompareTo(right) > 0;

    public static bool operator <=(MesCalendario left, MesCalendario right) => left.CompareTo(right) <= 0;

    public static bool operator >=(MesCalendario left, MesCalendario right) => left.CompareTo(right) >= 0;

    public override string ToString() => $"{Anio:0000}-{Mes:00}";
}
