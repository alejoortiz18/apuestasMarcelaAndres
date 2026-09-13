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
public sealed class LectorQrObservadorActividad : Activity, Camera.IPreviewCallback, ISurfaceHolderCallback
{
    public const int Peticion = 4731;

    private const int PixelesMinimos = 200_000;
    private const int PixelesMaximos = 800_000;
    private const int FramesQueSeSaltan = 3;

    private FrameLayout? _raiz;
    private SurfaceView? _vista;
    private Camera? _camara;
    private ISurfaceHolder? _holder;
    private byte[]? _bufferCamara;
    private byte[]? _frame;
    private volatile bool _listo;
    private volatile bool _cerrado;
    private bool _vistaAjustada;
    private int _ancho;
    private int _alto;
    private int _ocupado;
    private int _salto;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        _raiz = new FrameLayout(this)
        {
            LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent)
        };
        _vista = new SurfaceView(this);
        _vista.Holder!.AddCallback(this);
        _raiz.AddView(_vista, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent)
        {
            Gravity = GravityFlags.Center
        });

        var cancelar = new global::Android.Widget.Button(this)
        {
            Text = PdaTexts.Cancelar,
            TextSize = 16
        };
        cancelar.Click += (_, _) => Cerrar(null);
        var paramsBoton = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = GravityFlags.Bottom | GravityFlags.CenterHorizontal,
            BottomMargin = 48
        };
        _raiz.AddView(cancelar, paramsBoton);

        var aviso = new TextView(this)
        {
            Text = PdaTexts.ObservadorCamaraLeyendo,
            TextSize = 18,
            Gravity = GravityFlags.Center
        };
        aviso.SetTextColor(global::Android.Graphics.Color.White);
        aviso.SetPadding(24, 16, 24, 16);
        aviso.SetBackgroundColor(global::Android.Graphics.Color.Argb(180, 0, 0, 0));
        var paramsAviso = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = GravityFlags.Top | GravityFlags.CenterHorizontal,
            TopMargin = 36,
            LeftMargin = 24,
            RightMargin = 24
        };
        _raiz.AddView(aviso, paramsAviso);
        SetContentView(_raiz);
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
        if (data is null)
        {
            return;
        }

        if (_cerrado || _listo || _ancho <= 0 || _alto <= 0)
        {
            Reencolar(camera, data);
            return;
        }

        if (Interlocked.Increment(ref _salto) % FramesQueSeSaltan != 0)
        {
            Reencolar(camera, data);
            return;
        }

        if (Interlocked.Exchange(ref _ocupado, 1) == 1)
        {
            Reencolar(camera, data);
            return;
        }

        var ancho = _ancho;
        var alto = _alto;
        var frame = _frame;
        var tamano = ObservadorLectorQrMlKit.TamanoNv21(ancho, alto);
        if (frame is null || frame.Length < tamano || data.Length < tamano)
        {
            Interlocked.Exchange(ref _ocupado, 0);
            Reencolar(camera, data);
            return;
        }

        // Se copia el frame y el buffer de la cámara se devuelve enseguida: así no se
        // asigna memoria por cada cuadro, que era lo que terminaba matando el proceso.
        Buffer.BlockCopy(data, 0, frame, 0, tamano);
        Reencolar(camera, data);
        _ = Task.Run(async () =>
        {
            try
            {
                var codigo = await ObservadorLectorQrMlKit.DesdeNv21Async(frame, ancho, alto).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(codigo))
                {
                    codigo = ObservadorLecturaQr.DesdeNv21(frame, ancho, alto);
                }

                if (string.IsNullOrWhiteSpace(codigo) || _cerrado || _listo)
                {
                    return;
                }

                _listo = true;
                if (!IsFinishing && !IsDestroyed)
                {
                    RunOnUiThread(() => Cerrar(codigo));
                }
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error(
                    ObservadorLectorQrMlKit.Etiqueta,
                    $"camara: {ex.GetType().Name} {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _ocupado, 0);
            }
        });
    }

    private void Reencolar(Camera? camera, byte[] data)
    {
        if (_cerrado)
        {
            return;
        }

        try
        {
            (camera ?? _camara)?.AddCallbackBuffer(data);
        }
        catch (Exception)
        {
        }
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
                return;
            }

            _camara = Camera.Open(0);
            if (_camara is null)
            {
                return;
            }

            var parametros = _camara.GetParameters();
            var preview = TamanoPreview(parametros);
            parametros.SetPreviewSize(preview.Width, preview.Height);
            parametros.PreviewFormat = global::Android.Graphics.ImageFormatType.Nv21;
            var focos = parametros.SupportedFocusModes;
            if (focos is not null && focos.Contains(Camera.Parameters.FocusModeContinuousPicture))
            {
                parametros.FocusMode = Camera.Parameters.FocusModeContinuousPicture;
            }
            else if (focos is not null && focos.Contains(Camera.Parameters.FocusModeContinuousVideo))
            {
                parametros.FocusMode = Camera.Parameters.FocusModeContinuousVideo;
            }
            else if (focos is not null && focos.Contains(Camera.Parameters.FocusModeAuto))
            {
                parametros.FocusMode = Camera.Parameters.FocusModeAuto;
            }

            _camara.SetParameters(parametros);
            _camara.SetDisplayOrientation(90);
            _ancho = preview.Width;
            _alto = preview.Height;
            AjustarVista(preview.Width, preview.Height);
            _camara.SetPreviewDisplay(holder);

            var tamano = ObservadorLectorQrMlKit.TamanoNv21(preview.Width, preview.Height);
            _frame = new byte[tamano];
            _bufferCamara = new byte[tamano];
            _camara.SetPreviewCallbackWithBuffer(this);
            _camara.AddCallbackBuffer(_bufferCamara);
            _camara.StartPreview();
            global::Android.Util.Log.Info(
                ObservadorLectorQrMlKit.Etiqueta,
                $"camara: preview {preview.Width}x{preview.Height}");
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error(
                ObservadorLectorQrMlKit.Etiqueta,
                $"camara abrir: {ex.GetType().Name} {ex.Message}");
            SoltarCamara();
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

    private static Camera.Size TamanoPreview(Camera.Parameters parametros)
    {
        Camera.Size? elegido = null;
        foreach (var size in parametros.SupportedPreviewSizes ?? [])
        {
            var pixeles = size.Width * size.Height;
            if (pixeles < PixelesMinimos || pixeles > PixelesMaximos)
            {
                continue;
            }

            if (elegido is null || pixeles > elegido.Width * elegido.Height)
            {
                elegido = size;
            }
        }

        return elegido ?? parametros.PreviewSize;
    }

    private void SoltarCamara()
    {
        try
        {
            _camara?.SetPreviewCallbackWithBuffer(null);
            _camara?.StopPreview();
            _camara?.Release();
        }
        catch (Exception)
        {
        }

        _camara = null;
        _bufferCamara = null;
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
}
