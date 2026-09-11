using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Auth;
using NewRich.Application.Services;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;
using NewRich.Shared.Results;

namespace NewRich.UnitTests;

public sealed class AndroidPdaServiceTests
{
    [Fact]
    public async Task LoginMob_incluye_el_grupo_del_vendedor_desde_la_base()
    {
        var (sut, db, auth) = CreateSut();
        var usuarioId = Guid.NewGuid();
        var grupo = new Grupo { GrupoId = Guid.NewGuid(), Nombre = "Grupo Norte", FechaCreacion = DateTime.UtcNow };
        db.Grupos.Add(grupo);
        db.Usuarios.Add(new Usuario
        {
            UsuarioId = usuarioId,
            NombreCompleto = "Camila Rojas",
            NombreUsuario = "crojas",
            PasswordHash = "x",
            PasswordSalt = "x",
            Rol = RolUsuario.Vendedor
        });
        db.UsuariosGrupos.Add(new UsuarioGrupo { UsuarioId = usuarioId, GrupoId = grupo.GrupoId, Grupo = grupo });
        await db.SaveChangesAsync();

        auth.Setup(a => a.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<LoginResponse>.Ok(new LoginResponse
            {
                Token = "t",
                UsuarioId = usuarioId,
                NombreUsuario = "crojas",
                NombreCompleto = "Camila Rojas",
                Rol = RolUsuario.Vendedor
            }, SuccessMessages.OperacionExitosa));

        var resultado = await sut.LoginMobAsync(new LoginRequest { Usuario = "crojas", Password = "x", CodigoDispositivo = "PDA-042" }, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Data!.GrupoNombre.Should().Be("Grupo Norte");
        resultado.Data.NombreCompleto.Should().Be("Camila Rojas");
    }

    [Fact]
    public async Task DescargarOfflineMob_marca_generados_del_pda_y_no_entrega_utilizados()
    {
        var (sut, db, _) = CreateSut();
        var usuarioId = Guid.NewGuid();
        var dispositivoId = Guid.NewGuid();
        db.CodigosPreventaOffline.AddRange(
            new CodigoPreventaOffline
            {
                CodigoId = Guid.NewGuid(),
                ConsecutivoUnico = "OFF-000001",
                UsuarioId = usuarioId,
                DispositivoId = dispositivoId,
                PayloadCifrado = [1, 2, 3],
                EstadoDelCodigo = EstadoCodigoOffline.Generado,
                FechaCreacion = DateTime.UtcNow
            },
            new CodigoPreventaOffline
            {
                CodigoId = Guid.NewGuid(),
                ConsecutivoUnico = "OFF-000002",
                UsuarioId = usuarioId,
                DispositivoId = dispositivoId,
                PayloadCifrado = [9],
                EstadoDelCodigo = EstadoCodigoOffline.Utilizado,
                FechaCreacion = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var resultado = await sut.DescargarOfflineMobAsync(usuarioId, dispositivoId, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Message.Should().Be(SuccessMessages.CodigosOfflineDescargados);
        resultado.Data.Should().ContainSingle(c => c.Consecutivo == "OFF-000001");
        resultado.Data.Should().NotContain(c => c.Consecutivo == "OFF-000002");
        (await db.CodigosPreventaOffline.SingleAsync(c => c.ConsecutivoUnico == "OFF-000001"))
            .EstadoDelCodigo.Should().Be(EstadoCodigoOffline.Descargado);
    }

    [Fact]
    public async Task DescargarOfflineMob_sin_dispositivo_falla()
    {
        var (sut, _, _) = CreateSut();

        var resultado = await sut.DescargarOfflineMobAsync(Guid.NewGuid(), null, CancellationToken.None);

        resultado.IsSuccess.Should().BeFalse();
        resultado.Message.Should().Be(AuthMessages.DispositivoNoAsociado);
    }

    [Fact]
    public async Task DescargarOfflineMob_sin_codigos_del_usuario_no_marca_ajenos()
    {
        var (sut, db, _) = CreateSut();
        var usuarioConCodigos = Guid.NewGuid();
        var usuarioSinCodigos = Guid.NewGuid();
        var dispositivoA = Guid.NewGuid();
        var dispositivoB = Guid.NewGuid();
        db.CodigosPreventaOffline.Add(new CodigoPreventaOffline
        {
            CodigoId = Guid.NewGuid(),
            ConsecutivoUnico = "OFF-000010",
            UsuarioId = usuarioConCodigos,
            DispositivoId = dispositivoA,
            PayloadCifrado = [1],
            EstadoDelCodigo = EstadoCodigoOffline.Generado,
            FechaCreacion = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var resultado = await sut.DescargarOfflineMobAsync(usuarioSinCodigos, dispositivoB, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Data.Should().BeEmpty();
        resultado.Message.Should().Be(UsuarioMessages.SinCodigosOfflineDisponibles);
        (await db.CodigosPreventaOffline.SingleAsync(c => c.ConsecutivoUnico == "OFF-000010"))
            .EstadoDelCodigo.Should().Be(EstadoCodigoOffline.Generado);
    }

    [Fact]
    public async Task DescargarOfflineMob_entrega_generados_aunque_esten_en_un_pda_anterior()
    {
        var (sut, db, _) = CreateSut();
        var usuarioId = Guid.NewGuid();
        var pdaAnterior = Guid.NewGuid();
        var pdaActual = Guid.NewGuid();
        db.CodigosPreventaOffline.Add(new CodigoPreventaOffline
        {
            CodigoId = Guid.NewGuid(),
            ConsecutivoUnico = "OFF-000011",
            UsuarioId = usuarioId,
            DispositivoId = pdaAnterior,
            PayloadCifrado = [4, 5],
            EstadoDelCodigo = EstadoCodigoOffline.Generado,
            FechaCreacion = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var resultado = await sut.DescargarOfflineMobAsync(usuarioId, pdaActual, CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Message.Should().Be(SuccessMessages.CodigosOfflineDescargados);
        resultado.Data.Should().ContainSingle(c => c.Consecutivo == "OFF-000011");
        var codigo = await db.CodigosPreventaOffline.SingleAsync(c => c.ConsecutivoUnico == "OFF-000011");
        codigo.EstadoDelCodigo.Should().Be(EstadoCodigoOffline.Descargado);
        codigo.DispositivoId.Should().Be(pdaActual);
        codigo.FechaDescarga.Should().NotBeNull();
    }

    [Fact]
    public async Task DescargarOfflineMob_no_entrega_descargados_de_otro_pda()
    {
        var (sut, db, _) = CreateSut();
        var usuarioId = Guid.NewGuid();
        db.CodigosPreventaOffline.Add(new CodigoPreventaOffline
        {
            CodigoId = Guid.NewGuid(),
            ConsecutivoUnico = "OFF-000012",
            UsuarioId = usuarioId,
            DispositivoId = Guid.NewGuid(),
            PayloadCifrado = [7],
            EstadoDelCodigo = EstadoCodigoOffline.Descargado,
            FechaCreacion = DateTime.UtcNow,
            FechaDescarga = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var resultado = await sut.DescargarOfflineMobAsync(usuarioId, Guid.NewGuid(), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Data.Should().BeEmpty();
        resultado.Message.Should().Be(UsuarioMessages.SinCodigosOfflineDisponibles);
        (await db.CodigosPreventaOffline.SingleAsync(c => c.ConsecutivoUnico == "OFF-000012"))
            .EstadoDelCodigo.Should().Be(EstadoCodigoOffline.Descargado);
    }

    private static (AndroidPdaService Sut, NewRichDbContext Db, Mock<IAuthService> Auth) CreateSut()
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var auth = new Mock<IAuthService>();
        var clock = new RelojFijo();
        var sut = new AndroidPdaService(
            auth.Object,
            Mock.Of<IVentaService>(),
            Mock.Of<ILoteriaService>(),
            Mock.Of<IResultadoService>(),
            Mock.Of<IPremioService>(),
            Mock.Of<IValidacionBoletoService>(),
            Mock.Of<IChatService>(),
            Mock.Of<IConfiguracionService>(),
            db,
            clock);
        return (sut, db, auth);
    }

    private sealed class RelojFijo : IClock
    {
        public DateTime UtcNow { get; } = new(2026, 9, 9, 16, 0, 0, DateTimeKind.Utc);
        public DateTime LocalNow => UtcNow;
    }
}
