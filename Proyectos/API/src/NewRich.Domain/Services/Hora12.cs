using System.Globalization;
using System.Text.RegularExpressions;

namespace NewRich.Domain.Services;

/// <summary>Convierte horas entre TimeSpan (24 h) y texto 12 h con AM/PM.</summary>
public static class Hora12
{
    private static readonly Regex Patron = new(
        @"^\s*(?<h>\d{1,2}):(?<m>\d{2})(?::(?<s>\d{2}))?\s*(?<ampm>AM|PM)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Formatear(TimeSpan hora)
    {
        var date = DateTime.Today.Add(hora);
        return date.ToString("h:mm tt", CultureInfo.InvariantCulture);
    }

    public static bool TryParse(string? texto, out TimeSpan hora)
    {
        hora = default;
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        // Acepta también HH:mm / HH:mm:ss por compatibilidad con la API y type=time.
        if (TimeSpan.TryParse(texto.Trim(), CultureInfo.InvariantCulture, out hora))
        {
            return hora >= TimeSpan.Zero && hora < TimeSpan.FromDays(1);
        }

        var m = Patron.Match(texto);
        if (!m.Success)
        {
            return false;
        }

        var h = int.Parse(m.Groups["h"].Value, CultureInfo.InvariantCulture);
        var min = int.Parse(m.Groups["m"].Value, CultureInfo.InvariantCulture);
        var seg = m.Groups["s"].Success
            ? int.Parse(m.Groups["s"].Value, CultureInfo.InvariantCulture)
            : 0;
        if (h is < 1 or > 12 || min is < 0 or > 59 || seg is < 0 or > 59)
        {
            return false;
        }

        var ampm = m.Groups["ampm"].Value.ToUpperInvariant();
        if (h == 12)
        {
            h = 0;
        }

        if (ampm == "PM")
        {
            h += 12;
        }

        hora = new TimeSpan(h, min, seg);
        return true;
    }
}
