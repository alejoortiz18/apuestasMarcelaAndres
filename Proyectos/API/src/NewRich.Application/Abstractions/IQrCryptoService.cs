namespace NewRich.Application.Abstractions;

public sealed record QrPayload(
    Guid BoletoId,
    string CodigoPublico,
    string ClaveValidacion,
    int Version,
    Guid IdentificadorClave);

public interface IQrCryptoService
{
    string Encrypt(QrPayload payload);
    QrPayload? Decrypt(string qrContent);
    string HashClaveValidacion(string claveValidacion);
    string GenerarClaveValidacion();
}
