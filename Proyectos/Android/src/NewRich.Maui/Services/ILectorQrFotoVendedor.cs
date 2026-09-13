namespace NewRich.Maui.Services;

public interface ILectorQrFotoVendedor
{
    Task<string?> LeerAsync(byte[] imagen);
    Task<string?> LeerFotoSubidaAsync(byte[] imagen);
}
