using NewRich.Application.Contracts.Loterias;
using NewRich.Maui.Data;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;

namespace NewRich.Maui.Services;

public sealed class LoteriasEnVivoServicio
{
    private readonly ChatEnVivoServicio _vivo;
    private readonly NewRichApiClient _api;
    private readonly LocalDatabase _offline;
    private readonly SesionPda _sesion;
    private readonly ApiOpciones _opciones;
    private readonly ITokenStore _tokens;

    public event Action<IReadOnlyList<LoteriaResponse>>? CatalogoRefrescado;

    public LoteriasEnVivoServicio(
        ChatEnVivoServicio vivo,
        NewRichApiClient api,
        LocalDatabase offline,
        SesionPda sesion,
        ApiOpciones opciones,
        ITokenStore tokens)
    {
        _vivo = vivo;
        _api = api;
        _offline = offline;
        _sesion = sesion;
        _opciones = opciones;
        _tokens = tokens;
        _vivo.LoteriasActualizadas += OnAviso;
    }

    public async Task AsegurarSesionAsync(CancellationToken cancellationToken)
    {
        var token = await _tokens.ObtenerAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        try
        {
            await _vivo.AsegurarConectadoAsync(_opciones.BaseUrl, token, cancellationToken);
        }
        catch (Exception)
        {
        }
    }

    private void OnAviso() => _ = RefrescarAsync();

    private async Task RefrescarAsync()
    {
        try
        {
            var loterias = await _api.LoteriasAsync(CancellationToken.None);
            if (!loterias.IsSuccess || loterias.Data is null)
            {
                return;
            }

            await _offline.GuardarLoteriasAsync(loterias.Data);
            var operativa = await _api.OperativaAsync(CancellationToken.None);
            if (operativa.IsSuccess && operativa.Data is not null)
            {
                _sesion.Limites = operativa.Data;
            }

            MainThread.BeginInvokeOnMainThread(() => CatalogoRefrescado?.Invoke(loterias.Data));
        }
        catch (Exception)
        {
        }
    }
}
