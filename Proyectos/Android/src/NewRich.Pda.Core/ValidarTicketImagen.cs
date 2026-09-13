namespace NewRich.Pda.Core;

public static class ValidarTicketImagen
{
    public static string? CodigoDe(byte[]? imagen, out string? error)
    {
        var codigo = QrDesdeFoto.Leer(imagen ?? []);
        if (string.IsNullOrWhiteSpace(codigo))
        {
            error = PdaTexts.QrNoEncontradoEnFoto;
            return null;
        }

        error = null;
        return codigo;
    }
}
