namespace NewRich.Maui.Services;

public interface IEscanerQrCamara
{
    Task<string?> EscanearAsync();
}
