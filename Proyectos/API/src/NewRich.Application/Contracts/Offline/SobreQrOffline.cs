using System.IO.Compression;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NewRich.Domain.Enums;

namespace NewRich.Application.Contracts.Offline;

public sealed class LineaJugadaOffline
{
    public string Numero { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public IReadOnlyList<Guid> LoteriaIds { get; set; } = [];
    public IReadOnlyList<string> Loterias { get; set; } = [];
}

public sealed class JugadaOffline
{
    public string Tipo { get; set; } = TipoApuesta.INDIVIDUAL.ToString();
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    public IReadOnlyList<LineaJugadaOffline> Lineas { get; set; } = [];
}

public sealed class SobreQrOffline
{
    public string Codigo { get; set; } = string.Empty;
    public string Consecutivo { get; set; } = string.Empty;
    public JugadaOffline Jugada { get; set; } = new();
}

public static class SobreQrOfflineCodec
{
    public const string PrefijoTirilla = "NR1.";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new FechaJugadaJson() }
    };

    public static string Armar(string codigoCifrado, string consecutivo, JugadaOffline jugada) =>
        JsonSerializer.Serialize(new SobreQrOffline
        {
            Codigo = codigoCifrado,
            Consecutivo = consecutivo,
            Jugada = jugada
        }, Json);

    public static string ParaTirilla(string codigoCifrado, string consecutivo, JugadaOffline jugada)
    {
        var utf8 = Encoding.UTF8.GetBytes(Armar(codigoCifrado, consecutivo, jugada));
        using var buffer = new MemoryStream();
        using (var deflate = new DeflateStream(buffer, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            deflate.Write(utf8);
        }

        var b64 = Convert.ToBase64String(buffer.ToArray())
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return PrefijoTirilla + b64;
    }

    public static bool TryLeer(string? qr, out SobreQrOffline sobre)
    {
        sobre = new SobreQrOffline();
        if (string.IsNullOrWhiteSpace(qr))
        {
            return false;
        }

        foreach (var candidato in Candidatos(qr))
        {
            if (TryDeserializar(candidato, out sobre))
            {
                return true;
            }
        }

        sobre = new SobreQrOffline();
        return false;
    }

    private static IEnumerable<string> Candidatos(string qr)
    {
        var texto = qr.Trim();
        yield return texto;
        if (TryCompacto(texto, out var compacto))
        {
            yield return compacto;
        }

        yield return RepararImpreso(texto);
        if (TryBase64(texto, out var desdeBase64))
        {
            yield return desdeBase64;
        }

        var reparado = RepararImpreso(texto);
        if (TryBase64(reparado, out var base64Reparado))
        {
            yield return base64Reparado;
        }
    }

    private static bool TryDeserializar(string texto, out SobreQrOffline sobre)
    {
        sobre = new SobreQrOffline();
        var json = texto.Trim();
        if (json.Length == 0 || json[0] != '{')
        {
            return false;
        }

        try
        {
            var leido = JsonSerializer.Deserialize<SobreQrOffline>(json, Json);
            if (leido is null
                || string.IsNullOrWhiteSpace(leido.Codigo)
                || string.IsNullOrWhiteSpace(leido.Consecutivo)
                || leido.Jugada.Lineas.Count == 0)
            {
                return false;
            }

            sobre = leido;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryBase64(string texto, out string json)
    {
        json = string.Empty;
        var limpio = texto.Trim()
            .Replace('¡', '+')
            .Replace('¿', '=')
            .Replace('-', '/')
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Replace(" ", string.Empty);
        if (limpio.Length < 8 || limpio[0] == '{')
        {
            return false;
        }

        try
        {
            json = Encoding.UTF8.GetString(Convert.FromBase64String(limpio));
            return json.TrimStart().StartsWith('{');
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool TryCompacto(string texto, out string json)
    {
        json = string.Empty;
        var t = texto.Trim();
        if (!t.StartsWith(PrefijoTirilla, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var b64 = t[PrefijoTirilla.Length..]
            .Replace('\'', '-')
            .Replace('?', '_')
            .Replace('¿', '_')
            .Replace('¡', '-')
            .Replace('-', '+')
            .Replace('_', '/')
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty)
            .Replace(" ", string.Empty);
        var resto = b64.Length % 4;
        if (resto == 1)
        {
            return false;
        }

        if (resto == 2)
        {
            b64 += "==";
        }
        else if (resto == 3)
        {
            b64 += "=";
        }

        try
        {
            var raw = Convert.FromBase64String(b64);
            using var entrada = new MemoryStream(raw);
            using var deflate = new DeflateStream(entrada, CompressionMode.Decompress);
            using var salida = new MemoryStream();
            deflate.CopyTo(salida);
            json = Encoding.UTF8.GetString(salida.ToArray());
            return json.TrimStart().StartsWith('{');
        }
        catch (Exception ex) when (ex is FormatException or InvalidDataException or IOException)
        {
            return false;
        }
    }

    private sealed class FechaJugadaJson : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var texto = reader.GetString();
            if (DateTimeOffset.TryParse(texto, out var conZona))
            {
                return conZona.DateTime;
            }

            if (DateTime.TryParse(texto, out var local))
            {
                return local;
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value);
    }

    public static string RepararImpreso(string qr)
    {
        var s = qr.Trim();
        s = Regex.Replace(s, @"\}u002[Bb]", "+");
        s = Regex.Replace(s, @"\}u00ED", "í", RegexOptions.IgnoreCase);
        s = s.Replace('¿', '=');
        s = Regex.Replace(s, @"(\w+)\[Ñ´¨", "\"$1\":[{");
        s = Regex.Replace(s, @"(\w+)\[Ñ¨", "\"$1\":{");
        s = Regex.Replace(s, @"(\w+)\[Ñ´\[", "\"$1\":[\"");
        s = Regex.Replace(s, @"(\w+)\[Ñ\[", "\"$1\":\"");
        s = Regex.Replace(s, @"(\w+)\[Ñ", "\"$1\":");
        s = s.Replace("[,[", "\",\"");
        s = s.Replace("[+,", "\"],\"");
        s = s.Replace("[+*+**", "\"]}]}}");
        s = s.Replace("[+", "\"]");
        s = s.Replace('¨', '{');
        s = s.Replace('´', '[');
        s = s.Replace("*", "}");
        s = s.Replace('Ñ', ':');
        s = s.Replace('\'', '-');
        s = s.Replace("{[\"", "{\"");
        s = Regex.Replace(s, @",\[""", ",\"");
        s = s.Replace("\"[\"", "\"");
        s = s.Replace("\"\"", "\"");
        return CerrarJson(s);
    }

    private static string CerrarJson(string s)
    {
        if (s.Length == 0 || s[0] != '{')
        {
            s = "{" + s.TrimStart('[', '{');
        }

        var cierra = new StringBuilder();
        var pila = new Stack<char>();
        var enTexto = false;
        var escape = false;
        foreach (var c in s)
        {
            if (enTexto)
            {
                if (escape)
                {
                    escape = false;
                    continue;
                }

                if (c == '\\')
                {
                    escape = true;
                    continue;
                }

                if (c == '"')
                {
                    enTexto = false;
                }

                continue;
            }

            if (c == '"')
            {
                enTexto = true;
                continue;
            }

            if (c == '{')
            {
                pila.Push('}');
            }
            else if (c == '[')
            {
                pila.Push(']');
            }
            else if ((c == '}' || c == ']') && pila.Count > 0)
            {
                pila.Pop();
            }
        }

        if (enTexto)
        {
            cierra.Append('"');
        }

        while (pila.Count > 0)
        {
            cierra.Append(pila.Pop());
        }

        return s + cierra;
    }
}
