using NewRich.Constants.Messages;

namespace NewRich.Admin.Services;

/// <summary>Los avisos de chat van en el ícono de chat; la campana muestra los demás.</summary>
public static class AvisosChat
{
    public static bool EsChat(string? tipo) =>
        string.Equals(tipo, ChatMessages.TipoAvisoSoporte, StringComparison.Ordinal)
        || string.Equals(tipo, ChatMessages.TipoAvisoSoporteTecnico, StringComparison.Ordinal);
}
