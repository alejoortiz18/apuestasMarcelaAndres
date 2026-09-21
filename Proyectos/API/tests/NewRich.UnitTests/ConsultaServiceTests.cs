using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Contracts.Consultas;
using NewRich.Application.Contracts.Offline;
using NewRich.Application.Services;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class ConsultaServiceTests
{
    [Fact]
    public async Task BuscarAsync_con_prefijo_aol_encuentra_el_boleto()
    {
        var (sut, boleto) = await CrearBoletoAsync("4839201");

        var result = await sut.BuscarAsync(new BusquedaAdministrativaRequest { CodigoBoleto = "AOL-4839201" }, CancellationToken.None);

        result.Data.Should().ContainSingle(b => b.BoletoId == boleto.BoletoId);
    }

    [Fact]
    public async Task BuscarAsync_con_codigo_de_siete_digitos_encuentra_el_boleto()
    {
        var (sut, boleto) = await CrearBoletoAsync("4839201");

        var result = await sut.BuscarAsync(new BusquedaAdministrativaRequest { CodigoBoleto = "4839201" }, CancellationToken.None);

        result.Data.Should().ContainSingle(b => b.BoletoId == boleto.BoletoId);
    }

    [Fact]
    public async Task BuscarAsync_con_consecutivo_offline_encuentra_el_boleto()
    {
        var (sut, db, boleto) = await CrearContextoAsync("4839201");
        db.CodigosPreventaOffline.Add(new CodigoPreventaOffline
        {
            CodigoId = boleto.BoletoId,
            ConsecutivoUnico = "OFF-000020",
            UsuarioId = boleto.Venta!.UsuarioId,
            DispositivoId = Guid.NewGuid(),
            EstadoDelCodigo = EstadoCodigoOffline.Utilizado,
            FechaCreacion = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await sut.BuscarAsync(new BusquedaAdministrativaRequest { CodigoBoleto = "OFF-000020" }, CancellationToken.None);

        result.Data.Should().ContainSingle(b => b.BoletoId == boleto.BoletoId);
    }

    [Fact]
    public async Task BuscarAsync_con_los_digitos_del_consecutivo_offline_encuentra_el_boleto()
    {
        var (sut, db, boleto) = await CrearContextoAsync("1699284");
        db.CodigosPreventaOffline.Add(new CodigoPreventaOffline
        {
            CodigoId = boleto.BoletoId,
            ConsecutivoUnico = "OFF-000020",
            UsuarioId = boleto.Venta!.UsuarioId,
            DispositivoId = Guid.NewGuid(),
            EstadoDelCodigo = EstadoCodigoOffline.Utilizado,
            FechaCreacion = DateTime.UtcNow,
            VentaId = boleto.VentaId
        });
        await db.SaveChangesAsync();

        var result = await sut.BuscarAsync(new BusquedaAdministrativaRequest { CodigoBoleto = "000020" }, CancellationToken.None);

        result.Data.Should().ContainSingle(b => b.BoletoId == boleto.BoletoId && b.CodigoPublico == "1699284");
    }

    [Fact]
    public async Task BuscarAsync_con_off_impreso_en_el_qr_encuentra_el_boleto()
    {
        var (sut, db, boleto) = await CrearContextoAsync("4839201");
        boleto.QrCifrado = SobreQrOfflineCodec.Armar("1.clave.nonce.cipher.tag", "OFF-000018", new JugadaOffline());
        await db.SaveChangesAsync();

        var result = await sut.BuscarAsync(new BusquedaAdministrativaRequest { CodigoBoleto = "off-000018" }, CancellationToken.None);

        result.Data.Should().ContainSingle(b => b.BoletoId == boleto.BoletoId);
    }

    private static async Task<(ConsultaService Sut, Boleto Boleto)> CrearBoletoAsync(string codigoPublico)
    {
        var (sut, _, boleto) = await CrearContextoAsync(codigoPublico);
        return (sut, boleto);
    }

    private static async Task<(ConsultaService Sut, NewRichDbContext Db, Boleto Boleto)> CrearContextoAsync(string codigoPublico)
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var vendedor = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "Laura Gil",
            NombreUsuario = Guid.NewGuid().ToString("N")[..8],
            PasswordHash = "h",
            PasswordSalt = "s",
            Rol = RolUsuario.Vendedor,
            Estado = EstadoUsuario.Activo,
            FechaCreacion = DateTime.UtcNow
        };
        var venta = new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = vendedor.UsuarioId,
            FechaVenta = new DateTime(2026, 8, 30, 14, 25, 0, DateTimeKind.Utc),
            Total = 5000,
            TipoApuesta = TipoApuesta.COMBINADO
        };
        var boleto = new Boleto
        {
            BoletoId = Guid.NewGuid(),
            VentaId = venta.VentaId,
            CodigoPublico = codigoPublico,
            ClaveValidacionHash = "h",
            EstadoBoleto = EstadoBoleto.Jugado,
            FechaCreacion = venta.FechaVenta,
            VigenciaDias = 30
        };
        db.Usuarios.Add(vendedor);
        db.Ventas.Add(venta);
        db.Boletos.Add(boleto);
        await db.SaveChangesAsync();
        return (new ConsultaService(db), db, boleto);
    }
}
