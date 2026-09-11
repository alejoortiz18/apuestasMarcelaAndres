using System.Text;
using Android.Content;
using Android.OS;
using NewRich.Pda.Core;

namespace NewRich.Maui.Services;

internal static class ImpresoraInternaSenraise
{
    private const string Paquete = "recieptservice.com.recieptservice";
    private const string ClaseServicio = "recieptservice.com.recieptservice.service.PrinterService";
    private const string Descriptor = "recieptservice.com.recieptservice.PrinterInterface";

    public static Task<ResultadoImpresion> ImprimirAsync(string tirilla, string? contenidoQr) =>
        MainThread.InvokeOnMainThreadAsync(() => ImprimirEnHiloUi(tirilla, contenidoQr));

    private static async Task<ResultadoImpresion> ImprimirEnHiloUi(string tirilla, string? contenidoQr)
    {
        var actividad = Platform.CurrentActivity;
        if (actividad is null)
        {
            return ResultadoImpresion.Fallo(PdaTexts.ErrorImpresion);
        }

        var listo = new TaskCompletionSource<IBinder?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var conexion = new ConexionImpresora(listo);
        var intent = new Intent();
        intent.SetComponent(new ComponentName(Paquete, ClaseServicio));

        bool ligado;
        try
        {
            ligado = actividad.BindService(intent, conexion, Bind.AutoCreate);
        }
        catch (Exception)
        {
            return ResultadoImpresion.Fallo(PdaTexts.ErrorImpresion);
        }

        if (!ligado)
        {
            return ResultadoImpresion.Fallo(PdaTexts.ErrorImpresion);
        }

        IBinder? binder;
        try
        {
            binder = await listo.Task.WaitAsync(TimeSpan.FromSeconds(8));
        }
        catch (Exception)
        {
            Desligar(actividad, conexion);
            return ResultadoImpresion.Fallo(PdaTexts.ErrorImpresion);
        }

        if (binder is null)
        {
            Desligar(actividad, conexion);
            return ResultadoImpresion.Fallo(PdaTexts.ErrorImpresion);
        }

        try
        {
            var impresora = new ProxyImpresora(binder);
            var cuerpo = ImpresionTirilla.Cuerpo(tirilla);
            impresora.Intentar(() => impresora.SetAlignment(0));
            impresora.Intentar(() => impresora.SetTextSize(ImpresionTirilla.TamanoLetra));
            if (!string.IsNullOrWhiteSpace(cuerpo.Antes))
            {
                ImprimirTexto(impresora, cuerpo.Antes + "\n");
            }

            if (!string.IsNullOrWhiteSpace(contenidoQr))
            {
                impresora.Intentar(() => impresora.NextLine(1));
                impresora.Intentar(() => impresora.PrintEpson([0x1B, 0x61, 0x01]));
                impresora.Intentar(() => impresora.SetAlignment(1));
                impresora.Intentar(() => impresora.PrintQRCode(contenidoQr, ImpresionTirilla.ModuloQr, 1));
                impresora.Intentar(() => impresora.SetAlignment(0));
                impresora.Intentar(() => impresora.PrintEpson([0x1B, 0x61, 0x00]));
                impresora.Intentar(() => impresora.NextLine(1));
            }

            impresora.Intentar(() => impresora.SetTextSize(ImpresionTirilla.TamanoLetra));

            var pie = string.IsNullOrWhiteSpace(cuerpo.Despues)
                ? ImpresionTirilla.ParaImpresora(string.Empty)
                : ImpresionTirilla.ParaImpresora(cuerpo.Despues);
            ImprimirTexto(impresora, pie);
            await Task.Delay(400);
            return ResultadoImpresion.Correcta();
        }
        catch (Exception ex)
        {
            Android.Util.Log.Error("NewRichPrint", ex.ToString());
            return ResultadoImpresion.Fallo(PdaTexts.ErrorImpresion);
        }
        finally
        {
            Desligar(actividad, conexion);
        }
    }

    private static void ImprimirTexto(ProxyImpresora impresora, string texto)
    {
        if (!impresora.Intentar(() => impresora.PrintText(texto)))
        {
            impresora.PrintEpson(Encoding.UTF8.GetBytes(texto));
        }
    }

    private static void Desligar(Android.App.Activity actividad, IServiceConnection conexion)
    {
        try
        {
            actividad.UnbindService(conexion);
        }
        catch (Exception)
        {
        }
    }

    private sealed class ConexionImpresora : Java.Lang.Object, IServiceConnection
    {
        private readonly TaskCompletionSource<IBinder?> _listo;

        public ConexionImpresora(TaskCompletionSource<IBinder?> listo) => _listo = listo;

        public void OnServiceConnected(ComponentName? name, IBinder? service) =>
            _listo.TrySetResult(service);

        public void OnServiceDisconnected(ComponentName? name)
        {
        }
    }

    /// <summary>
    /// Códigos AIDL de PrinterInterface en el orden del servicio SRPrinter.
    /// </summary>
    private sealed class ProxyImpresora(IBinder remoto)
    {
        private const int CodigoPrintEpson = IBinder.FirstCallTransaction;
        private const int CodigoPrintText = IBinder.FirstCallTransaction + 2;
        private const int CodigoPrintQr = IBinder.FirstCallTransaction + 5;
        private const int CodigoSetAlignment = IBinder.FirstCallTransaction + 6;
        private const int CodigoSetTextSize = IBinder.FirstCallTransaction + 7;
        private const int CodigoNextLine = IBinder.FirstCallTransaction + 8;

        public bool Intentar(Action accion)
        {
            try
            {
                accion();
                return true;
            }
            catch (Exception ex)
            {
                Android.Util.Log.Warn("NewRichPrint", ex.ToString());
                return false;
            }
        }

        public void PrintEpson(byte[] datos) =>
            Transact(CodigoPrintEpson, data => data.WriteByteArray(datos));

        public void PrintText(string texto) => Transact(CodigoPrintText, data => data.WriteString(texto));

        public void PrintQRCode(string datos, int modulo, int correccion) =>
            Transact(CodigoPrintQr, data =>
            {
                data.WriteString(datos);
                data.WriteInt(modulo);
                data.WriteInt(correccion);
            });

        public void SetAlignment(int alineacion) =>
            Transact(CodigoSetAlignment, data => data.WriteInt(alineacion));

        public void SetTextSize(float tamano) =>
            Transact(CodigoSetTextSize, data => data.WriteFloat(tamano));

        public void NextLine(int lineas) =>
            Transact(CodigoNextLine, data => data.WriteInt(lineas));

        private void Transact(int codigo, Action<Parcel> escribir)
        {
            var data = Parcel.Obtain()!;
            var reply = Parcel.Obtain()!;
            try
            {
                data.WriteInterfaceToken(Descriptor);
                escribir(data);
                if (!remoto.Transact(codigo, data, reply, TransactionFlags.None))
                {
                    throw new InvalidOperationException("impresora");
                }

                try
                {
                    reply.ReadException();
                }
                catch (Exception)
                {
                }
            }
            finally
            {
                data.Recycle();
                reply.Recycle();
            }
        }
    }
}
