namespace NewRich.Application.Abstractions;

public interface IGeneradorLlaveFirma
{
    LlaveFirmaGenerada Generar(string alias, DateTime ahoraUtc);
}

/// <param name="Almacen">Archivo PKCS12 con la llave privada y su certificado.</param>
/// <param name="Clave">Contraseña del PKCS12 y de la llave.</param>
/// <param name="Huella">SHA-256 del certificado en hexadecimal, sin separadores.</param>
public sealed record LlaveFirmaGenerada(byte[] Almacen, string Clave, string Huella);
