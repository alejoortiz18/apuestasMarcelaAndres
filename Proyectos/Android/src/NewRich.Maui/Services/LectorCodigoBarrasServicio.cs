using Android.Content;
using Android.OS;
using NewRich.Pda.Core;

namespace NewRich.Maui.Services;

public sealed class LectorCodigoBarrasServicio : ILectorCodigoBarrasServicio
{
    private static readonly string[] Acciones =
    [
        "android.intent.ACTION_DECODE_DATA",
        "android.intent.action.SCANRESULT",
        "android.intent.action.SCAN_RESULT",
        "com.android.server.scannerservice.broadcast",
        "com.android.scanner.broadcast",
        "com.scan.onDecodeComplete",
        "nlscan.action.SCANNER_RESULT",
        "com.sunmi.scanner.ACTION_DATA_CODE_RECEIVED",
        "com.symbol.datawedge.api.RESULT_ACTION"
    ];

    private static readonly string[] Disparos =
    [
        "android.intent.action.SCANNER_BUTTON_DOWN",
        "com.scan.onStartScan",
        "com.android.server.scannerservice.broadcast.start",
        "nlscan.action.SCANNER_TRIG",
        "com.symbol.datawedge.api.ACTION"
    ];

    private Receptor? _receptor;
    private bool _activo;

    public event EventHandler<string>? CodigoLeido;

    public void Activar()
    {
        if (_activo)
        {
            return;
        }

        var contexto = Contexto();
        if (contexto is null)
        {
            return;
        }

        _receptor = new Receptor(this);
        var filtro = new IntentFilter();
        foreach (var accion in Acciones)
        {
            filtro.AddAction(accion);
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            contexto.RegisterReceiver(_receptor, filtro, ReceiverFlags.Exported);
        }
        else
        {
            contexto.RegisterReceiver(_receptor, filtro);
        }

        _activo = true;
    }

    public void Desactivar()
    {
        if (!_activo || _receptor is null)
        {
            return;
        }

        try
        {
            Contexto()?.UnregisterReceiver(_receptor);
        }
        catch (ArgumentException)
        {
        }

        _receptor.Dispose();
        _receptor = null;
        _activo = false;
    }

    public void Disparar()
    {
        var contexto = Contexto();
        if (contexto is null)
        {
            return;
        }

        foreach (var accion in Disparos)
        {
            var intent = new Intent(accion);
            if (accion.Contains("datawedge", StringComparison.OrdinalIgnoreCase))
            {
                intent.PutExtra("com.symbol.datawedge.api.SOFT_SCAN_TRIGGER", "START_SCANNING");
            }

            contexto.SendBroadcast(intent);
        }
    }

    private void Publicar(string codigo) =>
        MainThread.BeginInvokeOnMainThread(() => CodigoLeido?.Invoke(this, codigo));

    private static Android.Content.Context? Contexto() =>
        Platform.CurrentActivity ?? Android.App.Application.Context;

    private sealed class Receptor : BroadcastReceiver
    {
        private readonly LectorCodigoBarrasServicio _servicio;

        public Receptor(LectorCodigoBarrasServicio servicio) => _servicio = servicio;

        public override void OnReceive(Android.Content.Context? context, Intent? intent)
        {
            if (intent?.Extras is null)
            {
                return;
            }

            var extras = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var clave in intent.Extras.KeySet() ?? [])
            {
                extras[clave] = intent.Extras.Get(clave)?.ToString();
            }

            var codigo = LectorCodigoBarras.CodigoDe(extras);
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                _servicio.Publicar(codigo);
            }
        }
    }
}
