using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Auth;
using NewRich.Application.Services;
using NewRich.Constants;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Infrastructure.Persistence;
using NewRich.Infrastructure.Security;

namespace NewRich.UnitTests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_acepta_credenciales_validas_de_administrador()
    {
        var (sut, _, usuario, _) = await CreateSutConAdminAsync("Admin123");

        var result = await sut.LoginAsync(new LoginRequest
        {
            Usuario = usuario.NombreUsuario,
            Password = "Admin123"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Rol.Should().Be(RolUsuario.Administrador);
        result.Data.DebeCambiarPassword.Should().BeFalse();
        result.Data.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LoginAsync_marca_debe_cambiar_password_cuando_estado_validado()
    {
        var (sut, _, usuario, _) = await CreateSutConAdminAsync("Admin123", estadoValidado: true);

        var result = await sut.LoginAsync(new LoginRequest
        {
            Usuario = usuario.NombreUsuario,
            Password = "Admin123"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.DebeCambiarPassword.Should().BeTrue();
        result.Message.Should().Be(AuthMessages.DebeCambiarPassword);
    }

    [Fact]
    public async Task LoginAsync_rechaza_password_incorrecta()
    {
        var (sut, _, usuario, _) = await CreateSutConAdminAsync("Admin123");

        var result = await sut.LoginAsync(new LoginRequest
        {
            Usuario = usuario.NombreUsuario,
            Password = "Incorrecta1!"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.Message.Should().Be(AuthMessages.CredencialesInvalidas);
    }

    [Fact]
    public async Task CambiarPasswordAsync_exige_formato_fuerte()
    {
        var (sut, db, usuario, _) = await CreateSutConAdminAsync("Admin123", estadoValidado: true);
        var sesionId = await CrearSesionAsync(db, usuario.UsuarioId);

        var result = await sut.CambiarPasswordAsync(usuario.UsuarioId, sesionId, new CambiarPasswordRequest
        {
            PasswordActual = "Admin123",
            PasswordNuevo = "Admin1234",
            PasswordConfirmacion = "Admin1234"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(ValidationMessages.PasswordDebilFormato);
    }

    [Fact]
    public async Task CambiarPasswordAsync_actualiza_hash_y_limpia_estado_validado()
    {
        var (sut, db, usuario, _) = await CreateSutConAdminAsync("Admin123", estadoValidado: true);
        var sesionId = await CrearSesionAsync(db, usuario.UsuarioId);

        var result = await sut.CambiarPasswordAsync(usuario.UsuarioId, sesionId, new CambiarPasswordRequest
        {
            PasswordActual = "Admin123",
            PasswordNuevo = "Admin123*",
            PasswordConfirmacion = "Admin123*"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.DebeCambiarPassword.Should().BeFalse();

        var actualizado = await db.Usuarios.SingleAsync(u => u.UsuarioId == usuario.UsuarioId);
        actualizado.EstadoValidado.Should().BeFalse();

        var loginNuevo = await sut.LoginAsync(new LoginRequest
        {
            Usuario = usuario.NombreUsuario,
            Password = "Admin123*"
        }, CancellationToken.None);
        loginNuevo.IsSuccess.Should().BeTrue();

        var loginViejo = await sut.LoginAsync(new LoginRequest
        {
            Usuario = usuario.NombreUsuario,
            Password = "Admin123"
        }, CancellationToken.None);
        loginViejo.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task CambiarPasswordAsync_rechaza_password_actual_incorrecta()
    {
        var (sut, db, usuario, _) = await CreateSutConAdminAsync("Admin123", estadoValidado: true);
        var sesionId = await CrearSesionAsync(db, usuario.UsuarioId);

        var result = await sut.CambiarPasswordAsync(usuario.UsuarioId, sesionId, new CambiarPasswordRequest
        {
            PasswordActual = "OtraClave1!",
            PasswordNuevo = "Admin123*",
            PasswordConfirmacion = "Admin123*"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(AuthMessages.PasswordActualIncorrecto);
    }

    [Fact]
    public async Task ConfirmarAccionAdministrativaAsync_emite_token_con_password_correcta()
    {
        var (sut, _, usuario, _) = await CreateSutConAdminAsync("Admin123");

        var result = await sut.ConfirmarAccionAdministrativaAsync(usuario.UsuarioId, new ConfirmarAccionRequest
        {
            Password = "Admin123",
            Accion = AccionesProtegidas.UsuariosCrear
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ConfirmarAccionAdministrativaAsync_rechaza_password_incorrecta_sin_bloquear()
    {
        var (sut, db, usuario, _) = await CreateSutConAdminAsync("Admin123");

        var result = await sut.ConfirmarAccionAdministrativaAsync(usuario.UsuarioId, new ConfirmarAccionRequest
        {
            Password = "OtraClave1!",
            Accion = AccionesProtegidas.UsuariosCrear
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Message.Should().Be(AuthMessages.ContrasenaAccionIncorrecta);
        var actualizado = await db.Usuarios.SingleAsync(u => u.UsuarioId == usuario.UsuarioId);
        actualizado.EstadoBloqueado.Should().BeFalse();
        actualizado.IntentosFallidos.Should().Be(0);
    }

    [Fact]
    public async Task ConfirmarAccionAdministrativaAsync_token_solo_sirve_para_esa_accion_una_vez()
    {
        var (sut, _, usuario, store) = await CreateSutConAdminAsync("Admin123");

        var result = await sut.ConfirmarAccionAdministrativaAsync(usuario.UsuarioId, new ConfirmarAccionRequest
        {
            Password = "Admin123",
            Accion = AccionesProtegidas.UsuariosEliminar
        }, CancellationToken.None);

        store.Consumir(result.Data!.Token, usuario.UsuarioId, AccionesProtegidas.UsuariosCrear).Should().BeFalse();
        store.Consumir(result.Data.Token, usuario.UsuarioId, AccionesProtegidas.UsuariosEliminar).Should().BeTrue();
        store.Consumir(result.Data.Token, usuario.UsuarioId, AccionesProtegidas.UsuariosEliminar).Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmarAccionAdministrativaAsync_emite_token_con_usos_del_lote()
    {
        var (sut, _, usuario, store) = await CreateSutConAdminAsync("Admin123");

        var result = await sut.ConfirmarAccionAdministrativaAsync(usuario.UsuarioId, new ConfirmarAccionRequest
        {
            Password = "Admin123",
            Accion = AccionesProtegidas.PdaBloquear,
            Usos = 2
        }, CancellationToken.None);

        store.Consumir(result.Data!.Token, usuario.UsuarioId, AccionesProtegidas.PdaBloquear).Should().BeTrue();
        store.Consumir(result.Data.Token, usuario.UsuarioId, AccionesProtegidas.PdaBloquear).Should().BeTrue();
        store.Consumir(result.Data.Token, usuario.UsuarioId, AccionesProtegidas.PdaBloquear).Should().BeFalse();
    }

    private static async Task<(AuthService Sut, NewRichDbContext Db, Usuario Usuario, ConfirmacionAccionMemoria Store)> CreateSutConAdminAsync(
        string password,
        bool estadoValidado = false)
    {
        var options = new DbContextOptionsBuilder<NewRichDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new NewRichDbContext(options);
        var hasher = new Pbkdf2PasswordHasher();
        var hashed = hasher.Hash(password);
        var usuario = new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = "Administrador",
            NombreUsuario = "admin",
            PasswordHash = hashed.Hash,
            PasswordSalt = hashed.Salt,
            Rol = RolUsuario.Administrador,
            Estado = EstadoUsuario.Activo,
            EstadoValidado = estadoValidado,
            FechaCreacion = DateTime.UtcNow
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        var store = new ConfirmacionAccionMemoria();
        var sut = new AuthService(db, hasher, new JwtFalso(), new RelojFijo(new DateTime(2026, 9, 19, 20, 0, 0, DateTimeKind.Utc)), store);
        return (sut, db, usuario, store);
    }

    private static async Task<Guid> CrearSesionAsync(NewRichDbContext db, Guid usuarioId)
    {
        var sesion = new Sesion
        {
            SesionId = Guid.NewGuid(),
            UsuarioId = usuarioId,
            Token = Guid.NewGuid().ToString("N"),
            FechaInicio = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.AddHours(8),
            Activa = true
        };
        db.Sesiones.Add(sesion);
        await db.SaveChangesAsync();
        return sesion.SesionId;
    }

    private sealed class JwtFalso : IJwtTokenService
    {
        public string CreateToken(JwtUser user, DateTime expiresAt) => $"token:{user.UsuarioId:N}:{user.SesionId:N}";
    }

    private sealed class RelojFijo : IClock
    {
        public RelojFijo(DateTime utcNow) => UtcNow = utcNow;
        public DateTime UtcNow { get; }
        public DateTime LocalNow => UtcNow;
    }
}
