namespace NewRich.Maui.Services;

public interface ILectorCodigoBarrasServicio
{
    event EventHandler<string>? CodigoLeido;

    void Activar();

    void Desactivar();

    void Disparar();
}
