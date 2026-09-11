using NewRich.Application.Contracts.Offline;
using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;

namespace NewRich.Maui.Services;

public sealed class CodigosOfflineEnVivoServicio
{
    private readonly ChatEnVivoServicio _vivo;
    private readonly SesionPda _sesion;
    private readonly SincronizacionOfflineServicio _offline;
    private readonly ApiOpciones _opciones;
    private readonly ITokenStore _tokens;

    public CodigosOfflineEnVivoServicio(
        ChatEnVivoServicio vivo,
        SesionPda sesion,
        SincronizacionOfflineServicio offline,
        ApiOpciones opciones,
        ITokenStore tokens)
    {
        _vivo = vivo;
        _sesion = sesion;
        _offline = offline;
        _opciones = opciones;
        _tokens = tokens;
        _vivo.CodigosAsignados += OnAviso;
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
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }
    }

    public Task DesconectarAsync() => _vivo.DesconectarAsync();

    private void OnAviso(CodigosOfflineAsignadosAviso aviso)
    {
        var usuario = _sesion.Usuario;
        if (usuario is null
            || !DescargaCodigosOffline.AceptaAvisoEnVivo(usuario.DispositivoId, aviso.DispositivoId)
            || !DescargaCodigosOffline.SincronizarEnSilencio(true, usuario.Rol, usuario.DebeCambiarPassword))
        {
            return;
        }

        _ = _offline.SincronizarEnSilencioAsync(true, usuario.Rol, false, CancellationToken.None);
    }
}
