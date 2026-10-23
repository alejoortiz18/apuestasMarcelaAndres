using NewRich.Application.Contracts.Chat;

namespace NewRich.Vendedor.Services;

public static class ChatVista
{
    private static readonly string[] Imagenes = [".jpg", ".jpeg", ".png", ".webp"];

    public static (string Nombre, string Rol, string Iniciales, string Tono) Contraparte(ConversacionResponse conversacion, Guid yo)
    {
        var soyIniciador = conversacion.UsuarioIniciadorId == yo;
        var nombre = soyIniciador ? conversacion.NombreDestino : conversacion.NombreIniciador;
        var rol = soyIniciador ? conversacion.RolDestino : conversacion.RolIniciador;
        return (nombre, rol, Iniciales(nombre), Tono(nombre));
    }

    public static bool EsImagen(string? nombreArchivo) =>
        Imagenes.Contains(Path.GetExtension(nombreArchivo ?? string.Empty), StringComparer.OrdinalIgnoreCase);

    private static string Iniciales(string nombre)
    {
        var partes = (nombre ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 0)
        {
            return "?";
        }

        return partes.Length == 1
            ? partes[0][..Math.Min(2, partes[0].Length)].ToUpperInvariant()
            : $"{char.ToUpperInvariant(partes[0][0])}{char.ToUpperInvariant(partes[1][0])}";
    }

    private static string Tono(string nombre) =>
        string.IsNullOrEmpty(nombre) ? "gold" : (nombre[0] % 3) switch { 0 => "gold", 1 => "blue", _ => "green" };
}
