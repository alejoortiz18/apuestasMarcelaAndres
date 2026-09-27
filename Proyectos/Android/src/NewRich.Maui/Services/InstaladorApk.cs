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
        var intent = new global::Android.Content.Intent(global::Android.Content.Intent.ActionView);
        intent.SetDataAndType(uri, "application/vnd.android.package-archive");
        intent.SetFlags(global::Android.Content.ActivityFlags.NewTask | global::Android.Content.ActivityFlags.GrantReadUriPermission);
        contexto.StartActivity(intent);
#endif
    }
}
