using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Premios;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Infrastructure.Persistence;
using NewRich.Infrastructure.Security;

namespace NewRich.UnitTests;

public sealed class PremioServiceSqlTests
{
    [Fact]
    public async Task ReportarAsync_en_sql_server_crea_el_caso_sin_lanzar()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseSqlServer("Server=localhost;Database=NewRich;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        await using var db = new NewRichDbContext(options);
        await db.Database.OpenConnectionAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var boleto = await db.Boletos
            .Include(b => b.Venta)
            .FirstOrDefaultAsync(b => b.CodigoPublico == "5981759");
        if (boleto?.Venta is null)
        {
            return;
        }

        var vendedorId = boleto.Venta.UsuarioId;

        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Qr:MasterKey"]).Returns("B7E4C19A83D206F5E8A14C9B3D7F20E6A5C8B1D4E7F93A0C6B2D5E8F1A4C7D90");
        config.Setup(c => c["Qr:KeyId"]).Returns("8f2c1a6e-4b09-4d73-9e21-5a7c0b8d3f14");
        var crypto = new AesGcmQrCryptoService(config.Object);
        var clock = new ClockFijo(DateTime.UtcNow);
        var sut = new PremioService(
            db,
            clock,
            new NotificacionService(db, clock, new NotificacionTiempoRealNulo()),
            crypto,
            new ArchivosNulos(),
            new ValidacionBoletoService(db, crypto, clock));

        var result = await sut.ReportarAsync(vendedorId, new ReportarCasoGanadorRequest
        {
            TicketCode = "5981759"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue(result.Message);
        result.Message.Should().Be(SuccessMessages.CasoGanadorReportado);
    }

    private sealed class ClockFijo : IClock
    {
        public ClockFijo(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }

    private sealed class ArchivosNulos : IChatFileStorage
    {
        public Task DeleteAsync(string relativePath, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken cancellationToken) =>
            Task.FromResult<Stream?>(null);

        public Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken) =>
            Task.FromResult("premios/" + originalFileName);
    }
}
