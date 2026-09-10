using System.Security.Claims;

namespace NewRich.Shared;

public static class ClaimsUsuario
{
    public static bool TryId(ClaimsPrincipal? user, out Guid usuarioId)
    {
        usuarioId = default;
        if (user is null)
        {
            return false;
        }

        string[] tipos =
        [
            ClaimTypes.NameIdentifier,
            "sub",
            "nameid"
        ];

        foreach (var tipo in tipos)
        {
            var valor = user.FindFirst(tipo)?.Value;
            if (Guid.TryParse(valor, out usuarioId))
            {
                return true;
            }
        }

        foreach (var claim in user.Claims)
        {
            if (Guid.TryParse(claim.Value, out usuarioId) &&
                (claim.Type.Contains("nameidentifier", StringComparison.OrdinalIgnoreCase)
                 || claim.Type.Equals("sub", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }
}
