using FluentAssertions;
using NewRich.Application.Contracts.Usuarios;
using NewRich.Application.Services;
using NewRich.Domain.Enums;

namespace NewRich.UnitTests;

public sealed class GrupoAsignacionTests
{
    [Fact]
    public void VendedoresDisponibles_omite_administradores_observadores_y_miembros()
    {
        var enGrupo = Guid.NewGuid();
        var libre = Guid.NewGuid();
        var usuarios = new[]
        {
            new UsuarioResponse { UsuarioId = Guid.NewGuid(), Rol = RolUsuario.Administrador, NombreCompleto = "Admin" },
            new UsuarioResponse { UsuarioId = Guid.NewGuid(), Rol = RolUsuario.Observador, NombreCompleto = "Observador" },
            new UsuarioResponse { UsuarioId = enGrupo, Rol = RolUsuario.Vendedor, NombreCompleto = "Ya en grupo" },
            new UsuarioResponse { UsuarioId = libre, Rol = RolUsuario.Vendedor, NombreCompleto = "Disponible" }
        };

        var result = GrupoAsignacion.VendedoresDisponibles(usuarios, [enGrupo]);

        result.Should().ContainSingle(u => u.UsuarioId == libre);
    }
}
