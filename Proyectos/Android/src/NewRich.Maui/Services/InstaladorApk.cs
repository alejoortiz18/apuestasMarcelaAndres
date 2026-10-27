namespace NewRich.Maui.Services;

public static class InstaladorApk
{
    public static bool PuedeInstalar()
    {
#if ANDROID
        var contexto = Platform.CurrentActivity ?? global::Android.App.Application.Context;
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return true;
        }

        return contexto?.PackageManager?.CanRequestPackageInstalls() == true;
#else
        return false;
#endif
    }

    public static void PedirPermiso()
    {
#if ANDROID
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var contexto = Platform.CurrentActivity ?? global::Android.App.Application.Context;
        if (contexto is null)
        {
            return;
        }

        var ajustes = new global::Android.Content.Intent(global::Android.Provider.Settings.ActionManageUnknownAppSources);
        ajustes.SetData(global::Android.Net.Uri.Parse("package:" + contexto.PackageName));
        ajustes.SetFlags(global::Android.Content.ActivityFlags.NewTask);
        contexto.StartActivity(ajustes);
#endif
    }

    /// <summary>Huella SHA-256 del certificado y versionCode del APK descargado; null si Android no los puede leer.</summary>
    public static (string? Huella, long? NumeroCompilacion) LeerFirma(string ruta)
    {
#if ANDROID
        var paquetes = (Platform.CurrentActivity ?? global::Android.App.Application.Context)?.PackageManager;
        if (paquetes is null)
        {
            return (null, null);
        }

        byte[]? certificado;
        long? compilacion;
        if (OperatingSystem.IsAndroidVersionAtLeast(28))
        {
            var info = paquetes.GetPackageArchiveInfo(ruta, global::Android.Content.PM.PackageInfoFlags.SigningCertificates);
            certificado = info?.SigningInfo?.GetApkContentsSigners()?.FirstOrDefault()?.ToByteArray();
            compilacion = info?.LongVersionCode;
        }
        else
        {
#pragma warning disable CA1422 // En Android 27 o menor solo existe la lista de firmas.
            var info = paquetes.GetPackageArchiveInfo(ruta, global::Android.Content.PM.PackageInfoFlags.Signatures);
            certificado = info?.Signatures?.FirstOrDefault()?.ToByteArray();
            compilacion = info?.VersionCode;
#pragma warning restore CA1422
        }

        var huella = certificado is null
            ? null
            : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(certificado));
        return (huella, compilacion);
#else
        return (null, null);
#endif
    }

    public static void Abrir(string ruta)
    {
#if ANDROID
        var contexto = Platform.CurrentActivity ?? global::Android.App.Application.Context;
        if (contexto is null)
        {
            return;
        }

        var archivo = new Java.IO.File(ruta);
        var uri = AndroidX.Core.Content.FileProvider.GetUriForFile(contexto, contexto.PackageName + ".fileprovider", archivo);
        var intent = new global::Android.Content.Intent(NewRich.Pda.Core.Actualizacion.ApkDescargado.AccionInstalar);
        intent.SetDataAndType(uri, NewRich.Pda.Core.Actualizacion.ApkDescargado.TipoMime);
        if (NewRich.Pda.Core.Actualizacion.ApkDescargado.AdjuntarUriEnPortapapeles)
        {
            intent.ClipData = global::Android.Content.ClipData.NewRawUri(string.Empty, uri);
        }

        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask | global::Android.Content.ActivityFlags.GrantReadUriPermission);
        var resuelto = contexto.PackageManager?.ResolveActivity(intent, global::Android.Content.PM.PackageInfoFlags.MatchDefaultOnly);
        var paquete = resuelto?.ActivityInfo?.PackageName;
        if (!string.IsNullOrEmpty(paquete))
        {
            contexto.GrantUriPermission(paquete, uri, global::Android.Content.ActivityFlags.GrantReadUriPermission);
        }

        contexto.StartActivity(intent);
#endif
    }
}
