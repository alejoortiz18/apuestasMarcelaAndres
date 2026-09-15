namespace NewRich.Maui.Services;

public interface IEscanerQrObservador
{
    Task<string?> EscanearAsync(Func<string, Task>? alDetectarCodigoAsync = null);
}

public interface ILectorQrFotoObservador
{
    Task<string?> LeerAsync(byte[] imagen);

    Task<string?> LeerFotoSubidaAsync(byte[] imagen);
}
