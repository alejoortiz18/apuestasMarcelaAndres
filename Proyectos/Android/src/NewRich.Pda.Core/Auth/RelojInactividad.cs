using NewRich.Shared.Helpers;

namespace NewRich.Pda.Core.Auth;

public sealed class RelojInactividad
{
    private DateTime? _ultima;
    private int _minutos = InactividadSesion.MinutosPorDefecto;

    public void Iniciar(DateTime ahoraUtc) => Iniciar(ahoraUtc, InactividadSesion.MinutosPorDefecto);

    public void Iniciar(DateTime ahoraUtc, int minutos)
    {
        _ultima = ahoraUtc;
        _minutos = Normalizar(minutos);
    }

    public void ActualizarMinutos(int minutos)
    {
        if (_ultima.HasValue)
        {
            _minutos = Normalizar(minutos);
        }
    }

    public void RegistrarActividad(DateTime ahoraUtc)
    {
        if (_ultima.HasValue)
        {
            _ultima = ahoraUtc;
        }
    }

    public void Detener() => _ultima = null;

    public bool DebeCerrar(DateTime ahoraUtc) =>
        _ultima is DateTime ultima && InactividadSesion.Vencio(ultima, ahoraUtc, TimeSpan.FromMinutes(_minutos));

    private static int Normalizar(int minutos) => minutos < 1 ? InactividadSesion.MinutosPorDefecto : minutos;
}
