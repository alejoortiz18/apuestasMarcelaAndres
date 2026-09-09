using Microsoft.EntityFrameworkCore;
using NewRich.Application.Abstractions;
using NewRich.Application.Contracts.Grupos;
using NewRich.Constants.Messages;
using NewRich.Domain.Entities;
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Application.Services;

public sealed class GrupoService : IGrupoService
{
    private readonly INewRichDbContext _db;
    private readonly IClock _clock;

    public GrupoService(INewRichDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result<IReadOnlyList<GrupoResponse>>> ListarAsync(CancellationToken cancellationToken)
    {
        var grupos = await QueryGrupos()
            .OrderBy(g => g.Nombre)
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<GrupoResponse>>.Ok(grupos.Select(Map).ToList(), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<GrupoResponse>> ObtenerAsync(Guid grupoId, CancellationToken cancellationToken)
    {
        var grupo = await QueryGrupos().FirstOrDefaultAsync(g => g.GrupoId == grupoId, cancellationToken);
        return grupo is null
            ? Result<GrupoResponse>.Fail(UsuarioMessages.GrupoNoEncontrado, 404)
            : Result<GrupoResponse>.Ok(Map(grupo), SuccessMessages.OperacionExitosa);
    }

    public async Task<Result<GrupoResponse>> CrearAsync(CrearGrupoRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return Result<GrupoResponse>.Fail(ValidationMessages.CampoRequerido);
        }

        var nombre = request.Nombre.Trim();
        if (await _db.Grupos.AnyAsync(g => g.Nombre == nombre, cancellationToken))
        {
            return Result<GrupoResponse>.Fail(UsuarioMessages.GrupoNombreDuplicado, 409);
        }

        var grupo = new Grupo
        {
            GrupoId = Guid.NewGuid(),
            Nombre = nombre,
            Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim(),
            FechaCreacion = _clock.UtcNow
        };
        _db.Grupos.Add(grupo);
        await _db.SaveChangesAsync(cancellationToken);
        return Result<GrupoResponse>.Created(Map(grupo), SuccessMessages.RegistroCreado);
    }

    public async Task<Result<GrupoResponse>> ActualizarAsync(Guid grupoId, ActualizarGrupoRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return Result<GrupoResponse>.Fail(ValidationMessages.CampoRequerido);
        }

        var grupo = await QueryGrupos().FirstOrDefaultAsync(g => g.GrupoId == grupoId, cancellationToken);
        if (grupo is null)
        {
            return Result<GrupoResponse>.Fail(UsuarioMessages.GrupoNoEncontrado, 404);
        }

        var nombre = request.Nombre.Trim();
        if (await _db.Grupos.AnyAsync(g => g.Nombre == nombre && g.GrupoId != grupoId, cancellationToken))
        {
            return Result<GrupoResponse>.Fail(UsuarioMessages.GrupoNombreDuplicado, 409);
        }

        grupo.Nombre = nombre;
        grupo.Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        return Result<GrupoResponse>.Ok(Map(grupo), SuccessMessages.RegistroActualizado);
    }

    public async Task<Result> EliminarAsync(Guid grupoId, CancellationToken cancellationToken)
    {
        var grupo = await QueryGrupos().FirstOrDefaultAsync(g => g.GrupoId == grupoId, cancellationToken);
        if (grupo is null)
        {
            return Result.Fail(UsuarioMessages.GrupoNoEncontrado, 404);
        }

        if (grupo.UsuarioGrupos.Count > 0)
        {
            return Result.Fail(UsuarioMessages.GrupoConVendedoresAsociados);
        }

        _db.Grupos.Remove(grupo);
        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.RegistroEliminado);
    }

    public async Task<Result> AsignarVendedorAsync(Guid usuarioId, Guid? grupoId, CancellationToken cancellationToken)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, cancellationToken);
        if (usuario is null)
        {
            return Result.Fail(UsuarioMessages.UsuarioNoEncontrado, 404);
        }

        if (usuario.Rol != RolUsuario.Vendedor)
        {
            return Result.Fail(UsuarioMessages.SoloVendedoresPertenecenGrupos);
        }

        if (grupoId.HasValue)
        {
            var existe = await _db.Grupos.AnyAsync(g => g.GrupoId == grupoId.Value, cancellationToken);
            if (!existe)
            {
                return Result.Fail(UsuarioMessages.GrupoNoEncontrado, 404);
            }
        }

        var actuales = await _db.UsuariosGrupos.Where(x => x.UsuarioId == usuarioId).ToListAsync(cancellationToken);
        _db.UsuariosGrupos.RemoveRange(actuales);
        if (grupoId.HasValue)
        {
            _db.UsuariosGrupos.Add(new UsuarioGrupo
            {
                UsuarioId = usuarioId,
                GrupoId = grupoId.Value
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Ok(SuccessMessages.GrupoVendedorCambiado);
    }

    private IQueryable<Grupo> QueryGrupos() =>
        _db.Grupos.Include(g => g.UsuarioGrupos).ThenInclude(x => x.Usuario);

    private static GrupoResponse Map(Grupo grupo)
    {
        var vendedores = grupo.UsuarioGrupos
            .Where(x => x.Usuario is not null)
            .OrderBy(x => x.Usuario!.NombreCompleto)
            .Select(x => new GrupoVendedorResponse
            {
                UsuarioId = x.UsuarioId,
                NombreCompleto = x.Usuario!.NombreCompleto,
                Usuario = x.Usuario.NombreUsuario
            })
            .ToList();

        return new GrupoResponse
        {
            GrupoId = grupo.GrupoId,
            Nombre = grupo.Nombre,
            Descripcion = grupo.Descripcion,
            FechaCreacion = grupo.FechaCreacion,
            CantidadVendedores = vendedores.Count,
            Vendedores = vendedores
        };
    }
}
