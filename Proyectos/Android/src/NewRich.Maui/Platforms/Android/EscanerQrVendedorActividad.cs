using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics.Drawables;
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
public sealed class EscanerQrVendedorActividad : Activity, Camera.IPreviewCallback, ISurfaceHolderCallback, Camera.IAutoFocusCallback
{
    public const int Peticion = 4740;

    private FrameLayout? _raiz;
    private SurfaceView? _vista;
    private FrameLayout? _capaProceso;
    private Camera? _camara;
    private ISurfaceHolder? _holder;
    private byte[]? _bufferCamara;
    private byte[]? _bufferCamara2;
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

        var ayuda = new TextView(this)
        {
            Text = PdaTexts.ValidarQrAyuda,
            TextSize = 15,
            Gravity = GravityFlags.Center
        };
        ayuda.SetTextColor(global::Android.Graphics.Color.White);
        ayuda.SetPadding(28, 18, 28, 18);
        ayuda.SetBackgroundColor(global::Android.Graphics.Color.Argb(170, 0, 0, 0));
        _raiz.AddView(ayuda, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = GravityFlags.Top
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
            BottomMargin = 40
        });

        _capaProceso = CrearCapaProceso();
        _raiz.AddView(_capaProceso, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));

        _raiz.Click += (_, _) => Enfocar();
        _raiz.Clickable = true;
        SetContentView(_raiz);
        _ = Task.Run(VendedorLectorQrMlKit.Calentar);
        Registro($"abierto (mlkit={CamaraQrLectura.UsarMlKitEnVistaPrevia} inutilizable={VendedorLectorQrMlKit.Inutilizable})");
    }

    /// <summary>Tarjeta blanca centrada con spinner, al estilo de los avisos del sistema.</summary>
    private FrameLayout CrearCapaProceso()
    {
        var capa = new FrameLayout(this) { Visibility = ViewStates.Gone };
        capa.SetBackgroundColor(global::Android.Graphics.Color.Argb(120, 0, 0, 0));
        capa.Clickable = true;

        var tarjeta = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        tarjeta.SetGravity(GravityFlags.CenterVertical);
        tarjeta.SetPadding(48, 44, 60, 44);
        var fondo = new GradientDrawable();
        fondo.SetColor(global::Android.Graphics.Color.White);
        fondo.SetCornerRadius(40f);
        tarjeta.Background = fondo;

        tarjeta.AddView(
            new global::Android.Widget.ProgressBar(this) { Indeterminate = true },
            new LinearLayout.LayoutParams(64, 64) { RightMargin = 28 });

        var texto = new TextView(this)
        {
            Text = PdaTexts.LeyendoCodigo,
            TextSize = 18
        };
        texto.SetTextColor(global::Android.Graphics.Color.Argb(255, 17, 24, 39));
        tarjeta.AddView(texto, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent));

        capa.AddView(tarjeta, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent)
        {
            Gravity = GravityFlags.Center,
            LeftMargin = 32,
            RightMargin = 32
        });
        return capa;
    }

    private void MostrarLeyendoCodigo()
    {
        if (_capaProceso is null)
        {
            return;
        }

        _capaProceso.Visibility = ViewStates.Visible;
        _capaProceso.BringToFront();
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

        if (!_tamanoRegistrado)
        {
            _tamanoRegistrado = true;
            Registro($"cuadro {_ancho}x{_alto} bytes={data.Length} esperado={TamanoNv21(_ancho, _alto)}");
        }

        if (Interlocked.Increment(ref _salto) % CamaraQrLectura.SaltoDeCuadros != 0)
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
        var tamano = TamanoNv21(ancho, alto);
        if (data.Length < tamano)
        {
            Interlocked.Exchange(ref _ocupado, 0);
            Reencolar(camera, data);
            return;
        }

        // Copia exclusiva de esta lectura: el buffer de la cámara vuelve a la cola enseguida
        // y ML Kit nunca termina leyendo memoria que ya se reutilizó.
        var cuadro = new byte[tamano];
        Buffer.BlockCopy(data, 0, cuadro, 0, tamano);
        Reencolar(camera, data);
        var turno = Interlocked.Increment(ref _procesados);
        _ = Task.Run(async () =>
        {
            try
            {
                var reloj = System.Diagnostics.Stopwatch.StartNew();
                string? codigo = null;
                var msMlKit = 0L;
                if (CamaraQrLectura.UsarMlKitEnVistaPrevia && !VendedorLectorQrMlKit.Inutilizable)
                {
                    codigo = await VendedorLectorQrMlKit
                        .DesdeNv21Async(cuadro, ancho, alto, CamaraQrLectura.RotacionSensorGrados)
                        .ConfigureAwait(false);
                    msMlKit = reloj.ElapsedMilliseconds;
                }

                if (string.IsNullOrWhiteSpace(codigo))
                {
                    codigo = ObservadorLecturaQr.DesdeNv21(cuadro, ancho, alto);
                }

                if (turno % 4 == 0 || !string.IsNullOrWhiteSpace(codigo))
                {
                    Registro($"cuadro #{turno} mlkit={msMlKit}ms total={reloj.ElapsedMilliseconds}ms leido={!string.IsNullOrWhiteSpace(codigo)}");
                }

                if (string.IsNullOrWhiteSpace(codigo) || _cerrado || _listo)
                {
                    return;
                }

                _listo = true;
                Registro($"codigo leido, largo={codigo!.Length}");
                RunOnUiThread(async () =>
                {
                    MostrarLeyendoCodigo();
                    await Task.Delay(60);

                    if (EscanerQrNativo.AlDetectarCodigoAsync is not null)
                    {
                        try
                        {
                            await EscanerQrNativo.AlDetectarCodigoAsync(codigo);
                        }
                        catch (Exception ex)
                        {
                            Registro($"error en callback de validación: {ex.GetType().Name} {ex.Message}");
                        }
                    }

                    Cerrar(codigo);
                });
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
            parametros.SetPreviewSize(preview.Width, preview.Height);
            parametros.PreviewFormat = global::Android.Graphics.ImageFormatType.Nv21;
            AjustarEnfoque(parametros);
            _camara.SetParameters(parametros);
            _camara.SetDisplayOrientation(CamaraQrLectura.RotacionSensorGrados);
            var usado = _camara.GetParameters()?.PreviewSize ?? preview;
            _ancho = usado.Width;
            _alto = usado.Height;
            Registro($"cámara abierta preview={_ancho}x{_alto} enfoque={_camara.GetParameters()?.FocusMode} manual={_enfoqueManual}");
            AjustarVista(_ancho, _alto);
            _camara.SetPreviewDisplay(holder);

            var tamano = TamanoNv21(_ancho, _alto);
            _bufferCamara = new byte[tamano];
            _bufferCamara2 = new byte[tamano];
            _camara.SetPreviewCallbackWithBuffer(this);
            _camara.AddCallbackBuffer(_bufferCamara);
            _camara.AddCallbackBuffer(_bufferCamara2);
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
        _enfoqueManual = false;
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

    public void OnAutoFocus(bool success, Camera? camera)
    {
    }

    private void Enfocar()
    {
        if (!_enfoqueManual || _camara is null || _cerrado || _listo)
        {
            return;
        }

        try
        {
            _camara.AutoFocus(this);
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

    /// <summary>Vista previa lo más cercana posible al objetivo: cuadros chicos se decodifican rápido.</summary>
    private static Camera.Size TamanoPreview(Camera.Parameters parametros)
    {
        var tamanos = parametros.SupportedPreviewSizes ?? [];
        Camera.Size? elegido = null;
        var menorDif = long.MaxValue;
        foreach (var size in tamanos)
        {
            var pixeles = (long)size.Width * size.Height;
            if (pixeles < 150_000)
            {
                continue;
            }

            var dif = Math.Abs(pixeles - CamaraQrLectura.PixelesPreviewObjetivo);
            if (dif < menorDif)
            {
                menorDif = dif;
                elegido = size;
            }
        }

        return elegido ?? parametros.PreviewSize;
    }

    private static int TamanoNv21(int ancho, int alto) => (ancho * alto * 3) / 2;

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
        _bufferCamara2 = null;
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
