using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;
using NewRich.Maui.Platforms.Android;

namespace NewRich.Maui;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        EscanerQrNativo.Completar(requestCode, resultCode, data);
        LectorQrObservador.Completar(requestCode, resultCode, data);
    }
}
