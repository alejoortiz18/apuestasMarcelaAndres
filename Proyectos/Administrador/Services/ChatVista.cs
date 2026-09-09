using NewRich.Application.Contracts.Chat;

namespace NewRich.Admin.Services;

public static class ChatVista
{
    public static (string Nombre, string Rol, string Iniciales, string Tono) Contraparte(ConversacionResponse conversacion, Guid yo)
    {
        string nombre;
        string rol;
        if (conversacion.UsuarioIniciadorId == yo)
        {
            nombre = conversacion.NombreDestino;
            rol = conversacion.RolDestino;
        }
        else if (conversacion.UsuarioDestinoId == yo)
        {
            nombre = conversacion.NombreIniciador;
            rol = conversacion.RolIniciador;
        }
        else if (!string.Equals(conversacion.RolIniciador, "Administrador", StringComparison.Ordinal))
        {
            nombre = conversacion.NombreIniciador;
            rol = conversacion.RolIniciador;
        }
        else
        {
            nombre = conversacion.NombreDestino;
            rol = conversacion.RolDestino;
        }

        return (nombre, rol, Iniciales(nombre), Tono(nombre));
    }

    public static string Iniciales(string nombre)
    {
        var partes = (nombre ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 0)
        {
            return "?";
        }

        if (partes.Length == 1)
        {
            return partes[0][..Math.Min(2, partes[0].Length)].ToUpperInvariant();
        }

        return $"{char.ToUpperInvariant(partes[0][0])}{char.ToUpperInvariant(partes[1][0])}";
    }

    private static string Tono(string nombre)
    {
        if (string.IsNullOrEmpty(nombre))
        {
            return "gold";
        }

        return (nombre[0] % 3) switch
        {
            0 => "gold",
            1 => "blue",
            _ => "green"
        };
    }
}
