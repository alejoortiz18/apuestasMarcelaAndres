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
public sealed class LectorQrObservadorActividad : Activity, Camera.IPreviewCallback, ISurfaceHolderCallback, Camera.IAutoFocusCallback
{
    public const int Peticion = 4731;

    private FrameLayout? _raiz;
    private SurfaceView? _vista;
    private FrameLayout? _panelProceso;
    private Camera? _camara;
    private ISurfaceHolder? _holder;
    private byte[]? _bufferCamara;
    private byte[]? _bufferCamara2;
    private byte[]? _copiaLectura;
    private Handler? _reloj;
    private volatile bool _listo;
    private volatile bool _cerrado;
    private bool _vistaAjustada;
    private bool _enfoqueManual;
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

        _panelProceso = CrearCapaProceso();
        _raiz.AddView(_panelProceso, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));
        _raiz.Click += (_, _) => Enfocar();
        _raiz.Clickable = true;
        SetContentView(_raiz);
        _ = Task.Run(ObservadorLectorQrMlKit.Calentar);
    }

    /// <summary>Tarjeta blanca centrada con spinner, igual que en el escáner del vendedor.</summary>
    private FrameLayout CrearCapaProceso()
    {
        var capa = new FrameLayout(this) { Visibility = ViewStates.Gone };
        capa.SetBackgroundColor(global::Android.Graphics.Color.Argb(120, 0, 0, 0));
        capa.Clickable = true;

        var tarjeta = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        tarjeta.SetGravity(GravityFlags.CenterVertical);
        tarjeta.SetPadding(48, 44, 60, 44);
        var fondo = new global::Android.Graphics.Drawables.GradientDrawable();
        fondo.SetColor(global::Android.Graphics.Color.White);
        fondo.SetCornerRadius(40f);
        tarjeta.Background = fondo;

        tarjeta.AddView(
            new global::Android.Widget.ProgressBar(this) { Indeterminate = true },
            new LinearLayout.LayoutParams(64, 64) { RightMargin = 28 });

        var texto = new TextView(this)
        {
            Text = PdaTexts.ObservadorCamaraLeyendo,
            TextSize = 17
        };
        texto.SetTextColor(global::Android.Graphics.Color.Argb(255, 17, 24, 39));
        texto.SetMaxWidth((int)(Resources!.DisplayMetrics!.WidthPixels * 0.62));
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
        var tamano = ObservadorLectorQrMlKit.TamanoNv21(ancho, alto);
        if (data.Length < tamano)
        {
            Interlocked.Exchange(ref _ocupado, 0);
            Reencolar(camera, data);
            return;
        }

        // Copia aparte del buffer de la cámara, que vuelve a la cola enseguida. El arreglo se
        // reutiliza porque solo hay una lectura a la vez: pedir 1.4 MB por cuadro dejaba sin
        // memoria a los equipos chicos.
        var copia = _copiaLectura;
        if (copia is null || copia.Length != tamano)
        {
            Interlocked.Exchange(ref _ocupado, 0);
            Reencolar(camera, data);
            return;
        }

        Buffer.BlockCopy(data, 0, copia, 0, tamano);
        Reencolar(camera, data);
        _ = Task.Run(async () =>
        {
            try
            {
                string? codigo = null;
                var mlKitUsable = CamaraQrLectura.UsarMlKitEnVistaPrevia && !ObservadorLectorQrMlKit.Inutilizable;
                if (mlKitUsable)
                {
                    codigo = await ObservadorLectorQrMlKit.DesdeNv21Async(copia, ancho, alto).ConfigureAwait(false);
                }

                // El respaldo administrado (ZXing) es varias veces más lento que ML Kit en
                // hardware modesto: correrlo en cada cuadro tumba los cuadros por segundo.
                // Solo se usa si ML Kit no está disponible en este dispositivo.
                if (string.IsNullOrWhiteSpace(codigo) && !mlKitUsable)
                {
                    codigo = ObservadorLecturaQr.DesdeNv21(copia, ancho, alto);
                }

                if (string.IsNullOrWhiteSpace(codigo) || _cerrado || _listo)
                {
                    return;
                }

                _listo = true;
                if (!IsFinishing && !IsDestroyed)
                {
                    RunOnUiThread(async () =>
                    {
                        // Primero el aviso y luego la consulta: el spinner alcanza a pintarse
                        // antes de que la validación bloquee esta pantalla.
                        DetenerAnalisis();
                        if (_panelProceso is not null)
                        {
                            _panelProceso.Visibility = ViewStates.Visible;
                            _panelProceso.BringToFront();
                        }

                        await Task.Delay(120);

                        if (LectorQrObservador.AlDetectarCodigoAsync is not null)
                        {
                            try
                            {
                                await LectorQrObservador.AlDetectarCodigoAsync(codigo);
                            }
                            catch (Exception ex)
                            {
                                global::Android.Util.Log.Error(
                                    ObservadorLectorQrMlKit.Etiqueta,
                                    $"error callback observador: {ex.Message}");
                            }
                        }

                        Cerrar(codigo);
                    });
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

    /// <summary>Ya se leyó el código: la cámara deja de entregar cuadros mientras se valida.</summary>
    private void DetenerAnalisis()
    {
        try
        {
            _camara?.SetPreviewCallbackWithBuffer(null);
        }
        catch (Exception)
        {
        }
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
            AjustarEnfoque(parametros);
            _camara.SetParameters(parametros);
            _camara.SetDisplayOrientation(CamaraQrLectura.RotacionSensorGrados);
            // Lo que quedó aplicado, no lo pedido: si la cámara ajusta el tamaño, el buffer
            // debe seguirla o cada cuadro llega con menos bytes de los esperados.
            var usado = _camara.GetParameters()?.PreviewSize ?? preview;
            _ancho = usado.Width;
            _alto = usado.Height;
            AjustarVista(_ancho, _alto);
            _camara.SetPreviewDisplay(holder);

            var tamano = ObservadorLectorQrMlKit.TamanoNv21(_ancho, _alto);
            _bufferCamara = new byte[tamano];
            _bufferCamara2 = new byte[tamano];
            _copiaLectura = new byte[tamano];
            _camara.SetPreviewCallbackWithBuffer(this);
            _camara.AddCallbackBuffer(_bufferCamara);
            _camara.AddCallbackBuffer(_bufferCamara2);
            _camara.StartPreview();
            Enfocar();
            ProgramarEnfoque();
            global::Android.Util.Log.Info(
                ObservadorLectorQrMlKit.Etiqueta,
                $"camara: preview {_ancho}x{_alto} manual={_enfoqueManual}");
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error(
                ObservadorLectorQrMlKit.Etiqueta,
                $"camara abrir: {ex.GetType().Name} {ex.Message}");
            SoltarCamara();
        }
    }

    public void OnAutoFocus(bool success, Camera? camera)
    {
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

    /// <summary>
    /// La tirilla queda quieta frente al lente, así que el enfoque automático no se vuelve a
    /// disparar solo: se pide de nuevo cada cierto tiempo mientras se busca el QR.
    /// </summary>
    private void ProgramarEnfoque()
    {
        if (!_enfoqueManual || _cerrado || _listo)
        {
            return;
        }

        _reloj ??= new Handler(Looper.MainLooper!);
        _reloj.PostDelayed(
            () =>
            {
                Enfocar();
                ProgramarEnfoque();
            },
            CamaraQrLectura.MsEntreEnfoques);
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

    /// <summary>
    /// Vista previa grande: el QR de la tirilla es denso y con pocos píxeles por módulo el
    /// decodificador nunca lo resuelve, aunque en pantalla se vea bien.
    /// </summary>
    private static Camera.Size TamanoPreview(Camera.Parameters parametros)
    {
        var tamanos = parametros.SupportedPreviewSizes ?? [];
        var elegida = CamaraQrLectura.MejorPreview(tamanos.Select(t => (t.Width, t.Height)));
        if (elegida.Ancho <= 0)
        {
            return parametros.PreviewSize;
        }

        foreach (var size in tamanos)
        {
            if (size.Width == elegida.Ancho && size.Height == elegida.Alto)
            {
                return size;
            }
        }

        return parametros.PreviewSize;
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
        _bufferCamara2 = null;
        _copiaLectura = null;
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
