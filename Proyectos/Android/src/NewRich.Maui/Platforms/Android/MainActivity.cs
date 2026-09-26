using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Views;
using Microsoft.Extensions.DependencyInjection;
using NewRich.Maui.Platforms.Android;
using NewRich.Maui.Services;

namespace NewRich.Maui;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    WindowSoftInputMode = SoftInput.AdjustResize,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    public override bool DispatchTouchEvent(MotionEvent? e)
    {
        MarcarActividad();
        return base.DispatchTouchEvent(e);
    }

    public override bool DispatchKeyEvent(KeyEvent? e)
    {
        MarcarActividad();
        return base.DispatchKeyEvent(e);
    }

    private static void MarcarActividad() =>
        IPlatformApplication.Current?.Services.GetService<VigilanteInactividad>()?.MarcarActividad();

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        EscanerQrNativo.Completar(requestCode, resultCode, data);
        LectorQrObservador.Completar(requestCode, resultCode, data);
    }
}
