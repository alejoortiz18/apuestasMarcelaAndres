namespace NewRich.Domain.Enums;

public enum RolUsuario
{
    Administrador = 1,
    Vendedor = 2,
    Observador = 3,
    Super = 4
}

public static class RolConsola
{
    public static bool EsEquipoAdministrativo(RolUsuario rol) =>
        rol is RolUsuario.Administrador or RolUsuario.Super;
}
