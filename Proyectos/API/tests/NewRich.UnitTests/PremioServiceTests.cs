using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Contracts.Premios;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;

namespace NewRich.UnitTests;

public sealed class PremioServiceTests
{
    [Fact]
    public async Task ListarAsync_sin_casos_devuelve_lista_vacia()
    {
        var (sut, _) = CreateSut();

        var result = await sut.ListarAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task ReportarAsync_crea_caso_reportado_con_vendedor_y_pda()
    {
        var (sut, db) = CreateSut();
        var escenario = await CrearBoletoGanadorAsync(db);

        var result = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, new ReportarCasoGanadorRequest
        {
            TicketCode = escenario.Boleto.CodigoPublico
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Message.Should().Be(SuccessMessages.CasoGanadorReportado);
        result.Data!.Estado.Should().Be("Reportado");
        result.Data.Ticket.Should().Be(escenario.Boleto.CodigoPublico);
        result.Data.Vendedor.Should().Be("Laura Gil");
        result.Data.Pda.Should().Be("PDA-031");
        result.Data.Observador.Should().Be("Pendiente");
        db.CasosGanadores.Should().ContainSingle();
        db.Boletos.Single().CasoGanadorId.Should().Be(result.Data.CasoId);
        db.Notificaciones.Should().Contain(n => n.Tipo == "CasoGanador" && n.UsuarioId == escenario.Admin.UsuarioId);
    }

    [Fact]
    public async Task ReportarAsync_no_permite_un_segundo_caso_para_el_mismo_boleto()
    {
        var (sut, db) = CreateSut();
        var escenario = await CrearBoletoGanadorAsync(db);
        await sut.ReportarAsync(escenario.Vendedor.UsuarioId, new ReportarCasoGanadorRequest
        {
            TicketCode = escenario.Boleto.CodigoPublico
        }, CancellationToken.None);

        var result = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, new ReportarCasoGanadorRequest
        {
            TicketCode = escenario.Boleto.CodigoPublico
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(PremioMessages.CasoYaExiste);
    }

    [Fact]
    public async Task ValidarAsync_pasa_de_reportado_a_validado()
    {
        var (sut, db) = CreateSut();
        var escenario = await CrearBoletoGanadorAsync(db);
        var reporte = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, new ReportarCasoGanadorRequest
        {
            TicketCode = escenario.Boleto.CodigoPublico
        }, CancellationToken.None);

        var result = await sut.ValidarAsync(reporte.Data!.CasoId, escenario.Admin.UsuarioId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Estado.Should().Be("Validado");
        db.CasosGanadores.Single().AdminQueValido.Should().Be(escenario.Admin.UsuarioId);
    }

    [Fact]
    public async Task RechazarAsync_marca_el_caso_y_el_premio_como_rechazados()
    {
        var (sut, db) = CreateSut();
        var escenario = await CrearBoletoGanadorAsync(db);
        var reporte = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, new ReportarCasoGanadorRequest
        {
            TicketCode = escenario.Boleto.CodigoPublico
        }, CancellationToken.None);

        var result = await sut.RechazarAsync(reporte.Data!.CasoId, escenario.Admin.UsuarioId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Estado.Should().Be("Rechazado");
        db.Boletos.Single().EstadoDelPremio.Should().Be(EstadoDelPremio.Rechazado);
    }

    [Fact]
    public async Task AsignarAsync_asigna_observador_activo_y_notifica()
    {
        var (sut, db) = CreateSut();
        var escenario = await CrearBoletoGanadorAsync(db);
        var reporte = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, new ReportarCasoGanadorRequest
        {
            TicketCode = escenario.Boleto.CodigoPublico
        }, CancellationToken.None);
        await sut.ValidarAsync(reporte.Data!.CasoId, escenario.Admin.UsuarioId, CancellationToken.None);

        var result = await sut.AsignarAsync(reporte.Data.CasoId, escenario.Admin.UsuarioId, new AsignarObservadorRequest
        {
            ObservadorId = escenario.Observador.UsuarioId
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Estado.Should().Be("Asignado");
        result.Data.Observador.Should().Be("Andres Perez");
        db.Notificaciones.Should().Contain(n => n.UsuarioId == escenario.Observador.UsuarioId && n.Tipo == "CasoAsignado");
    }

    [Fact]
    public async Task AsignarAsync_rechaza_un_usuario_que_no_es_observador_activo()
    {
        var (sut, db) = CreateSut();
        var escenario = await CrearBoletoGanadorAsync(db);
        var reporte = await sut.ReportarAsync(escenario.Vendedor.UsuarioId, new ReportarCasoGanadorRequest
        {
            TicketCode = escenario.Boleto.CodigoPublico
        }, CancellationToken.None);
        await sut.ValidarAsync(reporte.Data!.CasoId, escenario.Admin.UsuarioId, CancellationToken.None);

        var result = await sut.AsignarAsync(reporte.Data.CasoId, escenario.Admin.UsuarioId, new AsignarObservadorRequest
        {
            ObservadorId = escenario.Vendedor.UsuarioId
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(PremioMessages.ObservadorInvalido);
    }

    private static (PremioService Sut, NewRichDbContext Db) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var clock = new FixedClock(new DateTime(2026, 8, 30, 19, 0, 0, DateTimeKind.Utc));
        return (new PremioService(db, clock), db);
    }

    private static async Task<EscenarioPremio> CrearBoletoGanadorAsync(NewRichDbContext db)
    {
        var admin = Usuario("Admin NewRich", RolUsuario.Administrador);
        var vendedor = Usuario("Laura Gil", RolUsuario.Vendedor);
        var observador = Usuario("Andres Perez", RolUsuario.Observador);
        var pda = new Dispositivo
        {
            DispositivoId = Guid.NewGuid(),
            CodigoDispositivo = "PDA-031",
            Tipo = TipoDispositivo.Vendedor,
            Estado = EstadoGeneral.Activo,
            FechaRegistro = DateTime.UtcNow
        };
        var venta = new Venta
        {
            VentaId = Guid.NewGuid(),
            UsuarioId = vendedor.UsuarioId,
            DispositivoId = pda.DispositivoId,
            FechaVenta = new DateTime(2026, 8, 30, 14, 25, 0, DateTimeKind.Utc),
            Total = 5000,
            TipoApuesta = TipoApuesta.COMBINADO
        };
        var boleto = new Boleto
        {
            BoletoId = Guid.NewGuid(),
            VentaId = venta.VentaId,
            CodigoPublico = "4839190",
            ClaveValidacionHash = "h",
            EstadoBoleto = EstadoBoleto.Ganador,
            FechaCreacion = DateTime.UtcNow,
            VigenciaDias = 30
        };
        db.Usuarios.AddRange(admin, vendedor, observador);
        db.Dispositivos.Add(pda);
        db.Ventas.Add(venta);
        db.Boletos.Add(boleto);
        await db.SaveChangesAsync();
        return new EscenarioPremio(admin, vendedor, observador, boleto);
    }

    private static Usuario Usuario(string nombre, RolUsuario rol) => new()
    {
        UsuarioId = Guid.NewGuid(),
        NombreCompleto = nombre,
        NombreUsuario = Guid.NewGuid().ToString("N")[..8],
        PasswordHash = "h",
        PasswordSalt = "s",
        Rol = rol,
        Estado = EstadoUsuario.Activo,
        FechaCreacion = DateTime.UtcNow
    };

    private sealed record EscenarioPremio(Usuario Admin, Usuario Vendedor, Usuario Observador, Boleto Boleto);

    private sealed class FixedClock : NewRich.Application.Abstractions.IClock
    {
        public FixedClock(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow.ToLocalTime();
    }
}
