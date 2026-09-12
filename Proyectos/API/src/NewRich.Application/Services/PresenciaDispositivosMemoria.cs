using System.Collections.Concurrent;
using NewRich.Application.Abstractions;
using NewRich.Shared;

namespace NewRich.Application.Services;

public sealed class PresenciaDispositivosMemoria : IPresenciaDispositivos
{
    private readonly ConcurrentDictionary<Guid, DateTime> _pulsos = new();
    private readonly ConcurrentDictionary<string, Guid> _conexiones = new();
    private readonly ConcurrentDictionary<Guid, int> _vivos = new();

    public void MarcarVivo(Guid dispositivoId, DateTime utcNow) =>
        _pulsos[dispositivoId] = utcNow;

    public void ConexionIniciada(Guid dispositivoId, string connectionId)
    {
        _conexiones[connectionId] = dispositivoId;
        _vivos.AddOrUpdate(dispositivoId, 1, (_, n) => n + 1);
    }

    public void ConexionTerminada(string connectionId)
    {
        if (!_conexiones.TryRemove(connectionId, out var dispositivoId))
        {
            return;
        }

        _vivos.AddOrUpdate(dispositivoId, 0, (_, n) => Math.Max(0, n - 1));
    }

    public bool EstaVivo(Guid dispositivoId, DateTime utcNow)
    {
        var conexiones = _vivos.TryGetValue(dispositivoId, out var n) ? n : 0;
        var pulso = _pulsos.TryGetValue(dispositivoId, out var t) ? t : (DateTime?)null;
        return PresenciaPda.EstaConectado(conexiones, pulso, utcNow);
    }
}
