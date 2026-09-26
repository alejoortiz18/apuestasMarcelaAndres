using NewRich.Shared.Helpers;

namespace NewRich.Pda.Core.Auth;

public sealed class RelojInactividad
{
    private DateTime? _ultima;

    public void Iniciar(DateTime ahoraUtc) => _ultima = ahoraUtc;

    public void RegistrarActividad(DateTime ahoraUtc)
    {
        if (_ultima.HasValue)
        {
            _ultima = ahoraUtc;
        }
    }

    public void Detener() => _ultima = null;

    public bool DebeCerrar(DateTime ahoraUtc) =>
        _ultima is DateTime ultima && InactividadSesion.Vencio(ultima, ahoraUtc);
}
