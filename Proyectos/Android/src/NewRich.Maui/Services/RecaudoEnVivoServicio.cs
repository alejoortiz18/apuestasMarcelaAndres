using NewRich.Application.Contracts.Recaudo;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;

namespace NewRich.Maui.Services;

public sealed class RecaudoEnVivoServicio
{
    private readonly ChatEnVivoServicio _vivo;
    private readonly SesionPda _sesion;
    private readonly ApiOpciones _opciones;
    private readonly ITokenStore _tokens;

    public event Action? CambioRecibido;

    public RecaudoEnVivoServicio(
        ChatEnVivoServicio vivo,
        SesionPda sesion,
        ApiOpciones opciones,
        ITokenStore tokens)
    {
        _vivo = vivo;
        _sesion = sesion;
        _opciones = opciones;
        _tokens = tokens;
        _vivo.RecaudoActualizado += OnAviso;
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

    private void OnAviso(RecaudoCambioAviso aviso)
    {
        if (!RecaudoEnVivo.Acepta(_sesion.Usuario?.UsuarioId, aviso))
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(() => CambioRecibido?.Invoke());
    }
}
