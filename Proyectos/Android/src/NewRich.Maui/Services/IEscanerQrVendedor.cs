namespace NewRich.Maui.Services;

public interface IEscanerQrVendedor
{
    Task<string?> EscanearAsync();
}
