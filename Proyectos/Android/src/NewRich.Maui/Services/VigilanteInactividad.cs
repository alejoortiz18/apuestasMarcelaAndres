using NewRich.Pda.Core;
using NewRich.Pda.Core.Api;
using NewRich.Pda.Core.Auth;

namespace NewRich.Maui.Services;

public sealed class VigilanteInactividad
{
    private readonly RelojInactividad _reloj = new();
    private readonly SesionPda _sesion;
    private readonly NewRichApiClient _api;
    private readonly ITokenStore _tokens;
    private readonly NavegadorApp _nav;
    private readonly CodigosOfflineEnVivoServicio _codigos;
    private readonly ChatEnVivoServicio _chat;
    private IDispatcherTimer? _temporizador;
    private bool _enSesion;
    private int _cerrando;

    public VigilanteInactividad(
        SesionPda sesion,
        NewRichApiClient api,
        ITokenStore tokens,
        NavegadorApp nav,
        CodigosOfflineEnVivoServicio codigos,
        ChatEnVivoServicio chat)
    {
        _sesion = sesion;
        _api = api;
        _tokens = tokens;
        _nav = nav;
        _codigos = codigos;
        _chat = chat;
    }

    public void MarcarActividad() => _reloj.RegistrarActividad(DateTime.UtcNow);

    public void Iniciar(IDispatcher despachador)
    {
        if (_temporizador is not null)
        {
            return;
        }

        _temporizador = despachador.CreateTimer();
        _temporizador.Interval = TimeSpan.FromSeconds(1);
        _temporizador.Tick += (_, _) => _ = RevisarAsync();
        _temporizador.Start();
    }

    private async Task RevisarAsync()
    {
        var ahora = DateTime.UtcNow;
        if (_sesion.Usuario is null)
        {
            if (_enSesion)
            {
                _reloj.Detener();
                _enSesion = false;
            }

            return;
        }

        if (!_enSesion)
        {
            _reloj.Iniciar(ahora);
            _enSesion = true;
            return;
        }

        if (!_reloj.DebeCerrar(ahora) || Interlocked.CompareExchange(ref _cerrando, 1, 0) != 0)
        {
            return;
        }

        try
        {
            _reloj.Detener();
            _enSesion = false;
            await _codigos.DesconectarAsync();
            await _chat.DesconectarAsync();
            await _api.LogoutAsync(CancellationToken.None);
            await _tokens.BorrarAsync();
            _sesion.Usuario = null;
            _sesion.Borrador = null;
            _sesion.AvisoInactividad = PdaTexts.SesionCerradaPorInactividad;
            _nav.IrALogin();
        }
        catch (Exception)
        {
            _sesion.Usuario = null;
            _sesion.Borrador = null;
            _sesion.AvisoInactividad = PdaTexts.SesionCerradaPorInactividad;
            _nav.IrALogin();
        }
        finally
        {
            Interlocked.Exchange(ref _cerrando, 0);
        }
    }
}
