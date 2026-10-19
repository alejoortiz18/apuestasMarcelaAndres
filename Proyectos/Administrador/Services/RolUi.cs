using NewRich.Admin.Constants;
using NewRich.Domain.Enums;

namespace NewRich.Admin.Services;

public static class RolUi
{
    public static string Nombre(RolUsuario rol) => rol switch
    {
        RolUsuario.VendedorWeb => UiTexts.RolVendedorWeb,
        _ => rol.ToString()
    };
}
