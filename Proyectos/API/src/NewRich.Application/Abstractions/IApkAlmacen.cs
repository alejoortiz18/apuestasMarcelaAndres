namespace NewRich.Application.Abstractions;

public interface IApkAlmacen
{
    Task GuardarAsync(string nombreArchivo, Stream contenido, long tamano, CancellationToken cancellationToken);
    Task EliminarAsync(string nombreArchivo, CancellationToken cancellationToken);
    Task<Stream?> AbrirAsync(string nombreArchivo, CancellationToken cancellationToken);
}

public sealed class ApkAlmacenException : Exception
{
    public ApkAlmacenException(string message) : base(message)
    {
    }
}
