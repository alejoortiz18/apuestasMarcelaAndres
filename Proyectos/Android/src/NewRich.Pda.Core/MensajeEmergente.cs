namespace NewRich.Pda.Core;

public sealed class MensajeEmergente
{
    private MensajeEmergente(string titulo, string cuerpo, string principal, string? secundario, bool muestraEquis)
    {
        Titulo = titulo;
        Cuerpo = cuerpo;
        Principal = principal;
        Secundario = secundario;
        MuestraEquis = muestraEquis;
    }

    public string Titulo { get; }
    public string Cuerpo { get; }
    public string Principal { get; }
    public string? Secundario { get; }
    public bool MuestraEquis { get; }
    public bool EsConfirmacion => Secundario is not null;

    public static MensajeEmergente Aviso(string titulo, string cuerpo, string principal) =>
        new(titulo, cuerpo, principal, null, false);

    public static MensajeEmergente AvisoConEquis(string titulo, string cuerpo, string principal) =>
        new(titulo, cuerpo, principal, null, true);

    public static MensajeEmergente Confirmar(string titulo, string cuerpo, string principal, string secundario) =>
        new(titulo, cuerpo, principal, secundario, false);
}
