using NewRich.Domain.Enums;

namespace NewRich.Admin.Services;

public static class EstadoUi
{
    public static string Pill(string estado)
    {
        return estado switch
        {
            "Activo" or "Jugado" or "Pagado" or "Pagado/cobrado" or "Leida" or "Validado" or "Registrado" or "Asignado" or "Premio entregado" or "Abierta" => "ok",
            "Ganador" or "Pendiente" or "Reportado" or "Por jugar" or "Utilizado" or "En proceso" => "warn",
            "Vencido" or "Bloqueado" or "Rechazado" or "Inactivo" or "No ganador" => "bad",
            _ => "info"
        };
    }

    public static string UsuarioEstado(bool bloqueado, EstadoUsuario estado) =>
        bloqueado ? "Bloqueado" : estado.ToString();

    public static string PdaEstado(EstadoGeneral estado) =>
        estado == EstadoGeneral.Inactivo ? "Bloqueado" : "Activo";
}
