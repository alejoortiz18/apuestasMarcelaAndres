using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Constants;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;

namespace NewRich.Application.Services;

public interface ISuperUsuarioAsegurador
{
    Task AsegurarAsync(CancellationToken cancellationToken);
}

public sealed class SuperUsuarioAsegurador : ISuperUsuarioAsegurador
{
    private readonly INewRichDbContext _db;
    private readonly IPasswordHasher _hasher;

    public SuperUsuarioAsegurador(INewRichDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task AsegurarAsync(CancellationToken cancellationToken)
    {
        var existe = await _db.Usuarios.AnyAsync(u => u.NombreUsuario == SuperUsuario.NombreUsuario, cancellationToken);
        if (existe)
        {
            return;
        }

        var hashed = _hasher.Hash(SuperUsuario.PasswordInicial);
        _db.Usuarios.Add(new Usuario
        {
            UsuarioId = Guid.NewGuid(),
            NombreCompleto = SuperUsuario.NombreCompleto,
            NombreUsuario = SuperUsuario.NombreUsuario,
            Alias = SuperUsuario.NombreUsuario,
            Email = SuperUsuario.Email,
            Documento = SuperUsuario.Documento,
            PasswordHash = hashed.Hash,
            PasswordSalt = hashed.Salt,
            Rol = RolUsuario.Super,
            Estado = EstadoUsuario.Activo,
            EstadoValidado = false,
            FechaCreacion = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
