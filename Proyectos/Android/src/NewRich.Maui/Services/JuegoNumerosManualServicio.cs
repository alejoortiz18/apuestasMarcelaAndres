using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Ventas;

namespace NewRich.Maui.Services;

/// <summary>Jugada en curso de «Juego números» y comprobación de conexión. Solo se juega en línea.</summary>
public sealed class JuegoNumerosManualServicio
{
    private readonly NewRichApiClient _api;

    public JuegoNumerosManualServicio(NewRichApiClient api) => _api = api;

    public JuegoNumerosManualBorrador? Borrador { get; set; }

    public ReciboJuegoNumerosManual? Recibo { get; set; }

    public async Task<bool> ConectarAsync(CancellationToken ct)
    {
        using var sondeo = CancellationTokenSource.CreateLinkedTokenSource(ct);
        sondeo.CancelAfter(PoliticaVentaPda.MsSondeoServidor);
        var conexion = await _api.ConectarAsync(
            PdaConexion.UrlsPara(DeviceInfo.Current.DeviceType == DeviceType.Virtual),
            sondeo.Token);
        return conexion.IsSuccess;
    }
}
