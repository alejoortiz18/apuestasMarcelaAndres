using NewRich.Constants.Messages;
using NewRich.Domain.Enums;
using NewRich.Shared.Results;

namespace NewRich.Pda.Core.Auth;

public enum ShellPda
{
    Vendedor = 1,
    Observador = 2
}

public static class NavegacionPorRol
{
    public static Result<ShellPda> Para(RolUsuario rol)
    {
        return rol switch
        {
            RolUsuario.Vendedor => Result<ShellPda>.Ok(ShellPda.Vendedor, string.Empty),
            RolUsuario.Observador => Result<ShellPda>.Ok(ShellPda.Observador, string.Empty),
            _ => Result<ShellPda>.Fail(AuthMessages.AdministradorNoOperaEnPda, 403)
        };
    }
}
