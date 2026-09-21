namespace NewRich.Application.Abstractions;

public interface IConfirmacionAccionStore
{
    string Emitir(Guid usuarioId, string accion, int usos = 1);
    bool Consumir(string token, Guid usuarioId, string accion);
}
