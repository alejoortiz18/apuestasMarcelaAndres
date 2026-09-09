using NewRich.Application.Contracts.Usuarios;
using NewRich.Domain.Enums;

namespace NewRich.Application.Services;

public static class GrupoAsignacion
{
    public static IReadOnlyList<UsuarioResponse> VendedoresDisponibles(
        IReadOnlyList<UsuarioResponse> usuarios,
        IReadOnlyCollection<Guid> miembrosGrupo)
    {
        return usuarios
            .Where(u => u.Rol == RolUsuario.Vendedor && !miembrosGrupo.Contains(u.UsuarioId))
            .OrderBy(u => u.NombreCompleto)
            .ToList();
    }
}
