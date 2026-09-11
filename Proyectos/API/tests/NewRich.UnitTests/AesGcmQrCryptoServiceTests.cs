using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using NewRich.Application.Abstractions;
using NewRich.Infrastructure.Security;

namespace NewRich.UnitTests;

public sealed class AesGcmQrCryptoServiceTests
{
    private const string QrImpresoDeEjemplo =
        "1.8f2c1a6e4b094d739e215a7c0b8d3f14.y8VAbWKe6RRrnWQg.p4PXxVxHTtGJmItZUM¡LoM-9XpuwGFLHE6VaoMbrhSdqEkCwgq0AYSV9aM0gb1g6NgjT8gunOsM05NQyEvkLlKoiVpOsQ7kpBEWshlTRwwu2h6uxDbyeoSlF1hhpBr6FGaeECxejkM-KPib1X03Yc1u5sf8uzeolV1AuoIMxHPR0X1IytCYOYmjsqcgDop2PFQkfkbfi76FTi-¡O5Dne¡cZ828hJfz¡UOfR3SWSU25L8ooYu0l26IgZWcuqxr9ozU4GIxPNYUHRzBQ¿¿.jX98CzzejFEopiSkfvn7sw¿¿";

    [Fact]
    public void Decrypt_recupera_el_boleto_cuando_el_qr_impreso_cambia_mas_barra_y_igual()
    {
        var sut = CrearSut();
        var payload = new QrPayload(Guid.NewGuid(), "2770755", "clave-prueba", 1, Guid.NewGuid());
        var canonico = sut.Encrypt(payload);
        var impreso = canonico.Replace("+", "¡", StringComparison.Ordinal)
            .Replace("/", "-", StringComparison.Ordinal)
            .Replace("=", "¿", StringComparison.Ordinal);

        var leido = sut.Decrypt(impreso);

        leido.Should().NotBeNull();
        leido!.BoletoId.Should().Be(payload.BoletoId);
        leido.CodigoPublico.Should().Be("2770755");
        leido.ClaveValidacion.Should().Be("clave-prueba");
    }

    [Fact]
    public void Decrypt_acepta_el_qr_impreso_real_del_ticket()
    {
        var sut = CrearSut();

        var leido = sut.Decrypt(QrImpresoDeEjemplo);

        leido.Should().NotBeNull();
        leido!.BoletoId.Should().Be(Guid.Parse("2370f44d-f0a6-4089-b7b0-43a1bcec9823"));
        leido.CodigoPublico.Should().Be("2770755");
    }

    private static AesGcmQrCryptoService CrearSut()
    {
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Qr:MasterKey"]).Returns("B7E4C19A83D206F5E8A14C9B3D7F20E6A5C8B1D4E7F93A0C6B2D5E8F1A4C7D90");
        config.Setup(c => c["Qr:KeyId"]).Returns("8f2c1a6e-4b09-4d73-9e21-5a7c0b8d3f14");
        return new AesGcmQrCryptoService(config.Object);
    }
}
