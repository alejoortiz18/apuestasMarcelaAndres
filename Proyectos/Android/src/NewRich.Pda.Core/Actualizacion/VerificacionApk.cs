using System.Security.Cryptography;
using NewRich.Application.Contracts.ApkStore;

namespace NewRich.Pda.Core.Actualizacion;

/// <param name="Sha256">SHA-256 del archivo descargado.</param>
/// <param name="HuellaFirma">SHA-256 del certificado con que viene firmado, o null si Android no lo pudo leer.</param>
/// <param name="NumeroCompilacion">versionCode del APK descargado, o null si Android no lo pudo leer.</param>
public sealed record DatosApkDescargado(string Sha256, string? HuellaFirma, long? NumeroCompilacion);

/// <summary>Revisa que el APK descargado sea el publicado en el almacén antes de abrir el instalador.</summary>
public static class VerificacionApk
{
    public static string? Problema(DatosApkDescargado descargado, VersionApkResponse esperada, int instalada)
    {
        if (!Igual(descargado.Sha256, esperada.Sha256))
        {
            return PdaTexts.ActualizacionArchivoAlterado;
        }

        if (descargado.HuellaFirma is null || !Igual(descargado.HuellaFirma, esperada.HuellaFirma))
        {
            return PdaTexts.ActualizacionFirmaDistinta;
        }

        if (descargado.NumeroCompilacion != esperada.NumeroCompilacion || esperada.NumeroCompilacion <= instalada)
        {
            return PdaTexts.ActualizacionVersionNoCorresponde;
        }

        return null;
    }

    public static string Sha256DeArchivo(string ruta)
    {
        using var archivo = File.OpenRead(ruta);
        return Convert.ToHexString(SHA256.HashData(archivo));
    }

    private static bool Igual(string a, string b) =>
        string.Equals(a.Replace(":", string.Empty), b.Replace(":", string.Empty), StringComparison.OrdinalIgnoreCase);
}
