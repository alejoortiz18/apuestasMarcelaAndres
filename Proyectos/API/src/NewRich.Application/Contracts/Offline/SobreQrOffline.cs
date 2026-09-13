using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
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
    public string Sello { get; set; } = string.Empty;
    public bool EsLlaveCorta { get; set; }
    public JugadaOffline Jugada { get; set; } = new();
}

public static class SobreQrOfflineCodec
{
    public const string PrefijoTirilla = "NR1.";
    public const string PrefijoTirillaCorta = "NR2.";
    public const string PrefijoTirillaCompacta = "NR3.";
    public const int LargoSello = 6;
    private const string Alfabeto32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

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

    public static string ParaTirilla(string codigoCifrado, string consecutivo, JugadaOffline jugada) =>
        PrefijoTirillaCompacta + ABase32(Empacar(codigoCifrado, consecutivo, jugada ?? new JugadaOffline()));

    public static string LlaveCorta(string codigoCifrado, string identificador) =>
        PrefijoTirillaCorta + identificador + "." + SelloDe(codigoCifrado, identificador);

    public static string ParaPapel(string? guardado, string identificadorPublico)
    {
        if (string.IsNullOrWhiteSpace(guardado))
        {
            return string.Empty;
        }

        if (TryLeer(guardado, out var sobre))
        {
            if (sobre.EsLlaveCorta)
            {
                return PrefijoTirillaCorta + sobre.Consecutivo + "." + sobre.Sello;
            }

            var id = string.IsNullOrWhiteSpace(sobre.Consecutivo) ? identificadorPublico : sobre.Consecutivo;
            return ParaTirilla(sobre.Codigo, id, sobre.Jugada);
        }

        return ParaTirilla(guardado, identificadorPublico, new JugadaOffline());
    }

    public static string SelloDe(string codigoCifrado, string identificador)
    {
        var raw = SHA256.HashData(Encoding.UTF8.GetBytes($"{codigoCifrado}|{identificador}"));
        return Convert.ToHexString(raw.AsSpan(0, LargoSello / 2)).ToLowerInvariant();
    }

    public static bool SelloCoincide(string codigoCifrado, string identificador, string? sello) =>
        !string.IsNullOrWhiteSpace(sello)
        && string.Equals(SelloDe(codigoCifrado, identificador), sello.Trim(), StringComparison.OrdinalIgnoreCase);

    public static bool TryLeer(string? qr, out SobreQrOffline sobre)
    {
        sobre = new SobreQrOffline();
        if (string.IsNullOrWhiteSpace(qr))
        {
            return false;
        }

        if (TryNr3(qr, out sobre) || TryNr2(qr, out sobre))
        {
            return true;
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

    private static bool TryNr3(string texto, out SobreQrOffline sobre)
    {
        sobre = new SobreQrOffline();
        var t = texto.Trim().ToUpperInvariant().Replace("'", string.Empty).Replace(" ", string.Empty);
        if (!t.StartsWith(PrefijoTirillaCompacta, StringComparison.Ordinal))
        {
            return false;
        }

        if (!DesdeBase32(t[PrefijoTirillaCompacta.Length..], out var raw) || !TryDesempacar(raw, out sobre))
        {
            sobre = new SobreQrOffline();
            return false;
        }

        return true;
    }

    private static byte[] Empacar(string codigoCifrado, string consecutivo, JugadaOffline jugada)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(1);
        EscribirTexto(ms, consecutivo);
        ms.WriteByte((byte)(string.Equals(jugada.Tipo, TipoApuesta.COMBINADO.ToString(), StringComparison.OrdinalIgnoreCase) ? 1 : 0));
        EscribirUInt32(ms, UnixDe(jugada.Fecha));
        EscribirUInt32(ms, (uint)Math.Max(0, decimal.Truncate(jugada.Total)));
        var lineas = jugada.Lineas ?? [];
        ms.WriteByte((byte)Math.Min(lineas.Count, 255));
        foreach (var linea in lineas.Take(255))
        {
            EscribirTexto(ms, linea.Numero);
            EscribirUInt32(ms, (uint)Math.Max(0, decimal.Truncate(linea.Valor)));
            var ids = linea.LoteriaIds ?? [];
            ms.WriteByte((byte)Math.Min(ids.Count, 255));
            foreach (var id in ids.Take(255))
            {
                ms.Write(id.ToByteArray());
            }
        }

        EscribirCifrado(ms, codigoCifrado);
        return ms.ToArray();
    }

    private static bool TryDesempacar(byte[] raw, out SobreQrOffline sobre)
    {
        sobre = new SobreQrOffline();
        var l = new Lector(raw);
        if (!l.TryByte(out var version) || version != 1
            || !l.TryTexto(out var consecutivo)
            || !l.TryByte(out var tipo)
            || !l.TryUInt32(out var unix)
            || !l.TryUInt32(out var total)
            || !l.TryByte(out var nLineas))
        {
            return false;
        }

        var lineas = new List<LineaJugadaOffline>(nLineas);
        for (var i = 0; i < nLineas; i++)
        {
            if (!l.TryTexto(out var numero) || !l.TryUInt32(out var valor) || !l.TryByte(out var nIds))
            {
                return false;
            }

            var ids = new List<Guid>(nIds);
            for (var j = 0; j < nIds; j++)
            {
                if (!l.TryBytes(16, out var guid))
                {
                    return false;
                }

                ids.Add(new Guid(guid));
            }

            lineas.Add(new LineaJugadaOffline { Numero = numero, Valor = valor, LoteriaIds = ids });
        }

        l.TryCifrado(out var codigo);

        sobre = new SobreQrOffline
        {
            Codigo = codigo,
            Consecutivo = consecutivo,
            Jugada = new JugadaOffline
            {
                Tipo = tipo == 1 ? TipoApuesta.COMBINADO.ToString() : TipoApuesta.INDIVIDUAL.ToString(),
                Fecha = unix == 0 ? default : DateTimeOffset.FromUnixTimeSeconds(unix).UtcDateTime,
                Total = total,
                Lineas = lineas
            }
        };
        return !string.IsNullOrWhiteSpace(sobre.Consecutivo);
    }

    private static void EscribirCifrado(MemoryStream ms, string codigoCifrado)
    {
        if (TryPartesAes(codigoCifrado, out var keyId, out var nonce, out var cipher, out var tag))
        {
            ms.WriteByte(1);
            ms.Write(keyId.ToByteArray());
            ms.Write(nonce);
            ms.Write(tag);
            EscribirUInt16(ms, (ushort)cipher.Length);
            ms.Write(cipher);
            return;
        }

        var utf8 = Encoding.UTF8.GetBytes(codigoCifrado);
        ms.WriteByte(0);
        EscribirUInt16(ms, (ushort)Math.Min(utf8.Length, ushort.MaxValue));
        ms.Write(utf8, 0, Math.Min(utf8.Length, ushort.MaxValue));
    }

    private static bool TryPartesAes(string codigo, out Guid keyId, out byte[] nonce, out byte[] cipher, out byte[] tag)
    {
        keyId = Guid.Empty;
        nonce = [];
        cipher = [];
        tag = [];
        var partes = (codigo ?? string.Empty).Split('.');
        if (partes.Length != 5 || partes[0] != "1" || !Guid.TryParseExact(partes[1], "N", out keyId))
        {
            return false;
        }

        try
        {
            nonce = Convert.FromBase64String(partes[2]);
            cipher = Convert.FromBase64String(partes[3]);
            tag = Convert.FromBase64String(partes[4]);
            return nonce.Length == 12 && tag.Length == 16 && cipher.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string RearmarAes(Guid keyId, byte[] nonce, byte[] cipher, byte[] tag) =>
        string.Join('.', "1", keyId.ToString("N"), Convert.ToBase64String(nonce), Convert.ToBase64String(cipher), Convert.ToBase64String(tag));

    private static void EscribirTexto(MemoryStream ms, string valor)
    {
        var utf8 = Encoding.UTF8.GetBytes(valor ?? string.Empty);
        var n = Math.Min(utf8.Length, 255);
        ms.WriteByte((byte)n);
        ms.Write(utf8, 0, n);
    }

    private static void EscribirUInt32(MemoryStream ms, uint valor)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(b, valor);
        ms.Write(b);
    }

    private static void EscribirUInt16(MemoryStream ms, ushort valor)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(b, valor);
        ms.Write(b);
    }

    private static uint UnixDe(DateTime fecha)
    {
        if (fecha == default)
        {
            return 0;
        }

        var utc = fecha.Kind == DateTimeKind.Local ? fecha.ToUniversalTime() : DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
        var unix = new DateTimeOffset(utc).ToUnixTimeSeconds();
        return unix < 0 ? 0 : (uint)Math.Min(unix, uint.MaxValue);
    }

    private static string ABase32(byte[] data)
    {
        if (data.Length == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = 0;
        var bits = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                sb.Append(Alfabeto32[(buffer >> bits) & 31]);
            }
        }

        if (bits > 0)
        {
            sb.Append(Alfabeto32[(buffer << (5 - bits)) & 31]);
        }

        return sb.ToString();
    }

    private static bool DesdeBase32(string texto, out byte[] data)
    {
        data = [];
        if (string.IsNullOrWhiteSpace(texto))
        {
            return false;
        }

        var mapa = new int[128];
        Array.Fill(mapa, -1);
        for (var i = 0; i < Alfabeto32.Length; i++)
        {
            mapa[Alfabeto32[i]] = i;
        }

        var buffer = 0;
        var bits = 0;
        var lista = new List<byte>(texto.Length);
        foreach (var c in texto)
        {
            if (c >= mapa.Length || mapa[c] < 0)
            {
                return false;
            }

            buffer = (buffer << 5) | mapa[c];
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                lista.Add((byte)((buffer >> bits) & 255));
            }
        }

        data = lista.ToArray();
        return data.Length > 0;
    }

    private sealed class Lector(byte[] data)
    {
        private int _i;

        public bool TryByte(out byte v)
        {
            v = 0;
            if (_i >= data.Length)
            {
                return false;
            }

            v = data[_i++];
            return true;
        }

        public bool TryUInt16(out ushort v)
        {
            v = 0;
            if (!TryBytes(2, out var b))
            {
                return false;
            }

            v = BinaryPrimitives.ReadUInt16BigEndian(b);
            return true;
        }

        public bool TryUInt32(out uint v)
        {
            v = 0;
            if (!TryBytes(4, out var b))
            {
                return false;
            }

            v = BinaryPrimitives.ReadUInt32BigEndian(b);
            return true;
        }

        public bool TryBytes(int n, out byte[] v)
        {
            v = [];
            if (n < 0 || _i + n > data.Length)
            {
                return false;
            }

            v = data.AsSpan(_i, n).ToArray();
            _i += n;
            return true;
        }

        public bool TryTexto(out string s)
        {
            s = string.Empty;
            if (!TryByte(out var len) || !TryBytes(len, out var raw))
            {
                return false;
            }

            s = Encoding.UTF8.GetString(raw);
            return true;
        }

        public bool TryCifrado(out string codigo)
        {
            codigo = string.Empty;
            if (!TryByte(out var tipo))
            {
                return false;
            }

            if (tipo == 1)
            {
                if (!TryBytes(16, out var key)
                    || !TryBytes(12, out var nonce)
                    || !TryBytes(16, out var tag)
                    || !TryUInt16(out var nCipher)
                    || !TryBytes(nCipher, out var cipher))
                {
                    return false;
                }

                codigo = RearmarAes(new Guid(key), nonce, cipher, tag);
                return true;
            }

            if (tipo != 0 || !TryUInt16(out var n) || !TryBytes(n, out var utf8))
            {
                return false;
            }

            codigo = Encoding.UTF8.GetString(utf8);
            return true;
        }
    }

    private static bool TryNr2(string texto, out SobreQrOffline sobre)
    {
        sobre = new SobreQrOffline();
        var t = texto.Trim().Replace('\'', '-');
        if (!t.StartsWith(PrefijoTirillaCorta, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var resto = t[PrefijoTirillaCorta.Length..];
        var ultimo = resto.LastIndexOf('.');
        if (ultimo <= 0 || ultimo == resto.Length - 1)
        {
            return false;
        }

        var identificador = resto[..ultimo].Trim();
        var sello = resto[(ultimo + 1)..].Trim();
        if (identificador.Length == 0 || sello.Length != LargoSello || !sello.All(Uri.IsHexDigit))
        {
            return false;
        }

        sobre = new SobreQrOffline
        {
            Consecutivo = identificador,
            Sello = sello,
            EsLlaveCorta = true
        };
        return true;
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
