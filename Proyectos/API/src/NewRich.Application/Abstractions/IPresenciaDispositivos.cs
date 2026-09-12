namespace NewRich.Application.Abstractions;

public interface IPresenciaDispositivos
{
    void MarcarVivo(Guid dispositivoId, DateTime utcNow);
    void ConexionIniciada(Guid dispositivoId, string connectionId);
    void ConexionTerminada(string connectionId);
    bool EstaVivo(Guid dispositivoId, DateTime utcNow);
}
