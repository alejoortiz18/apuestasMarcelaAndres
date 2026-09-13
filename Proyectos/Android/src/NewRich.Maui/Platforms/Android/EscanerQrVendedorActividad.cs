using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware;
using Android.OS;
using Android.Views;
using Android.Widget;
using NewRich.Pda.Core;
using Camera = Android.Hardware.Camera;

namespace NewRich.Maui.Platforms.Android;

[Activity(
    Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen",
    ScreenOrientation = ScreenOrientation.Portrait,
    Exported = false)]
public sealed class EscanerQrVendedorActividad : Activity, Camera.IPreviewCallback, ISurfaceHolderCallback
{
    public const int Peticion = 4740;

    private const int SaltoDeCuadros = 2;
    private const int CuadrosPorLectorInterno = 4;

    private FrameLayout? _raiz;
    private SurfaceView? _vista;
    private TextView? _aviso;
    private Camera? _camara;
    private ISurfaceHolder? _holder;
    private bool _listo;
    private bool _cerrado;
    private bool _vistaAjustada;
    private bool _enfoqueManual;
    private bool _tamanoRegistrado;
    private int _ancho;
    private int _alto;
    private int _ocupado;
    private int _salto;
    private int _procesados;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _raiz = new FrameLayout(this);
        _vista = new SurfaceView(this);
        _vista.Holder!.AddCallback(this);
        _raiz.AddView(_vista, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent)
        {
            Gravity = GravityFlags.Center
        });

        _aviso = new TextView(this)
        {
            Text = PdaTexts.LeyendoQr,
            TextSize = 18,
            Gravity = GravityFlags.Center
        };
        _aviso.SetTextColor(global::Android.Graphics.Color.White);
        _aviso.SetPadding(24, 20, 24, 20);
        _aviso.SetBackgroundColor(global::Android.Graphics.Color.Argb(180, 0, 0, 0));
        _raiz.AddView(_aviso, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = GravityFlags.Bottom,
            BottomMargin = 96
        });

        var cancelar = new global::Android.Widget.Button(this)
        {
            Text = PdaTexts.Cancelar,
            TextSize = 16
        };
        cancelar.Click += (_, _) => Cerrar(null);
        _raiz.AddView(cancelar, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = GravityFlags.Bottom | GravityFlags.CenterHorizontal,
            BottomMargin = 24
        });

        _raiz.Click += (_, _) => Enfocar();
        _raiz.Clickable = true;
        SetContentView(_raiz);
        Registro($"abierto (ml kit inutilizable={VendedorLectorQrMlKit.Inutilizable})");
    }

    public void SurfaceCreated(ISurfaceHolder holder)
    {
        _holder = holder;
        AbrirCamara(holder);
    }

    public void SurfaceChanged(ISurfaceHolder holder, global::Android.Graphics.Format format, int width, int height)
    {
        _holder = holder;
        if (_camara is null && !_cerrado)
        {
            AbrirCamara(holder);
        }
    }

    public void SurfaceDestroyed(ISurfaceHolder holder)
    {
        if (ReferenceEquals(_holder, holder))
        {
            _holder = null;
        }

        SoltarCamara();
    }

    public void OnPreviewFrame(byte[]? data, Camera? camera)
    {
        if (_cerrado || _listo || data is null || _ancho <= 0 || _alto <= 0)
        {
            return;
        }

        if (!_tamanoRegistrado)
        {
            _tamanoRegistrado = true;
            Registro($"cuadro {_ancho}x{_alto} bytes={data.Length} esperado={_ancho * _alto * 3 / 2}");
        }

        if (Interlocked.Increment(ref _salto) % SaltoDeCuadros != 0)
        {
            return;
        }

        if (Interlocked.Exchange(ref _ocupado, 1) == 1)
        {
            return;
        }

        var copia = new byte[data.Length];
        Buffer.BlockCopy(data, 0, copia, 0, data.Length);
        var ancho = _ancho;
        var alto = _alto;
        var turno = Interlocked.Increment(ref _procesados);
        _ = Task.Run(async () =>
        {
            try
            {
                var reloj = System.Diagnostics.Stopwatch.StartNew();
                var codigo = await VendedorLectorQrMlKit.DesdeNv21Async(copia, ancho, alto).ConfigureAwait(false);
                var msMlKit = reloj.ElapsedMilliseconds;
                var interno = false;
                if (string.IsNullOrWhiteSpace(codigo)
                    && (VendedorLectorQrMlKit.Inutilizable || turno % CuadrosPorLectorInterno == 0))
                {
                    interno = true;
                    codigo = VendedorLecturaQr.DesdeNv21(copia, ancho, alto, usarCpp: false);
                }

                if (turno % 4 == 0 || !string.IsNullOrWhiteSpace(codigo))
                {
                    Registro($"cuadro #{turno} mlkit={msMlKit}ms interno={interno} total={reloj.ElapsedMilliseconds}ms leido={!string.IsNullOrWhiteSpace(codigo)}");
                }

                if (string.IsNullOrWhiteSpace(codigo) || _cerrado)
                {
                    return;
                }

                _listo = true;
                Registro($"codigo leido, largo={codigo!.Length}");
                RunOnUiThread(() => Cerrar(codigo));
            }
            catch (Exception ex)
            {
                Registro($"error al decodificar: {ex.GetType().Name} {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _ocupado, 0);
            }
        });
    }

    protected override void OnResume()
    {
        base.OnResume();
        if (!_cerrado && _holder?.Surface is not null && _holder.Surface.IsValid)
        {
            AbrirCamara(_holder);
        }
    }

    protected override void OnPause()
    {
        SoltarCamara();
        base.OnPause();
    }

    private void AbrirCamara(ISurfaceHolder holder)
    {
        if (_cerrado || _camara is not null)
        {
            return;
        }

        try
        {
            if (Camera.NumberOfCameras <= 0)
            {
                Registro("sin cámaras disponibles");
                return;
            }

            _camara = Camera.Open(0);
            if (_camara is null)
            {
                Registro("Camera.Open devolvió nulo");
                return;
            }

            var parametros = _camara.GetParameters();
            if (parametros is null)
            {
                Registro("sin parámetros de cámara");
                SoltarCamara();
                return;
            }

            var preview = TamanoPreview(parametros);
            if (preview is not null)
            {
                parametros.SetPreviewSize(preview.Width, preview.Height);
            }

            parametros.PreviewFormat = global::Android.Graphics.ImageFormatType.Nv21;
            AjustarEnfoque(parametros);
            _camara.SetParameters(parametros);
            _camara.SetDisplayOrientation(90);
            var usado = _camara.GetParameters()?.PreviewSize ?? preview;
            _ancho = usado?.Width ?? 0;
            _alto = usado?.Height ?? 0;
            Registro($"cámara abierta preview={_ancho}x{_alto} enfoque={_camara.GetParameters()?.FocusMode}");
            AjustarVista(_ancho, _alto);
            _camara.SetPreviewDisplay(holder);
            _camara.SetPreviewCallback(this);
            _camara.StartPreview();
            Enfocar();
        }
        catch (Exception ex)
        {
            Registro($"error al abrir la cámara: {ex.GetType().Name} {ex.Message}");
            SoltarCamara();
        }
    }

    private void AjustarEnfoque(Camera.Parameters parametros)
    {
        var focos = parametros.SupportedFocusModes;
        if (focos is null || focos.Count == 0)
        {
            return;
        }

        if (focos.Contains(Camera.Parameters.FocusModeContinuousPicture))
        {
            parametros.FocusMode = Camera.Parameters.FocusModeContinuousPicture;
            return;
        }

        if (focos.Contains(Camera.Parameters.FocusModeContinuousVideo))
        {
            parametros.FocusMode = Camera.Parameters.FocusModeContinuousVideo;
            return;
        }

        if (focos.Contains(Camera.Parameters.FocusModeMacro))
        {
            parametros.FocusMode = Camera.Parameters.FocusModeMacro;
            _enfoqueManual = true;
            return;
        }

        if (focos.Contains(Camera.Parameters.FocusModeAuto))
        {
            parametros.FocusMode = Camera.Parameters.FocusModeAuto;
            _enfoqueManual = true;
        }
    }

    private void Enfocar()
    {
        if (_camara is null || _cerrado || !_enfoqueManual)
        {
            return;
        }

        try
        {
            _camara.CancelAutoFocus();
            _camara.AutoFocus(null);
        }
        catch (Exception)
        {
        }
    }

    private void AjustarVista(int previewAncho, int previewAlto)
    {
        if (_vistaAjustada || _vista is null || previewAncho <= 0 || previewAlto <= 0)
        {
            return;
        }

        var pantallaAncho = Resources!.DisplayMetrics!.WidthPixels;
        var pantallaAlto = Resources.DisplayMetrics.HeightPixels;
        var visualAncho = previewAlto;
        var visualAlto = previewAncho;
        var escala = Math.Max(pantallaAncho / (float)visualAncho, pantallaAlto / (float)visualAlto);
        var ancho = Math.Max(pantallaAncho, (int)(visualAncho * escala));
        var alto = Math.Max(pantallaAlto, (int)(visualAlto * escala));
        _vista.LayoutParameters = new FrameLayout.LayoutParams(ancho, alto)
        {
            Gravity = GravityFlags.Center
        };
        _vistaAjustada = true;
    }

    private static Camera.Size? TamanoPreview(Camera.Parameters parametros)
    {
        Camera.Size? elegido = null;
        foreach (var size in parametros.SupportedPreviewSizes ?? [])
        {
            var pixeles = size.Width * size.Height;
            if (pixeles < 300_000 || pixeles > 1_300_000)
            {
                continue;
            }

            if (elegido is null || pixeles > elegido.Width * elegido.Height)
            {
                elegido = size;
            }
        }

        return elegido;
    }

    private void SoltarCamara()
    {
        try
        {
            _camara?.SetPreviewCallback(null);
            _camara?.StopPreview();
            _camara?.Release();
        }
        catch (Exception)
        {
        }

        _camara = null;
    }

    private void Cerrar(string? codigo)
    {
        if (_cerrado)
        {
            return;
        }

        _cerrado = true;
        SoltarCamara();
        var data = new Intent();
        if (!string.IsNullOrWhiteSpace(codigo))
        {
            data.PutExtra("SCAN_RESULT", codigo);
            SetResult(Result.Ok, data);
        }
        else
        {
            Registro($"cerrado sin código, cuadros procesados={_procesados}");
            SetResult(Result.Canceled);
        }

        Finish();
    }

    protected override void OnDestroy()
    {
        if (!_cerrado)
        {
            Cerrar(null);
        }

        base.OnDestroy();
    }

    private static void Registro(string mensaje) =>
        global::Android.Util.Log.Info(VendedorLectorQrMlKit.Etiqueta, $"camara: {mensaje}");
}
