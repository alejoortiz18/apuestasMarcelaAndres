namespace NewRich.Maui.Services;

public interface IEscanerQrObservador
{
    Task<string?> EscanearAsync();
}

public interface ILectorQrFotoObservador
{
    Task<string?> LeerAsync(byte[] imagen);
}
