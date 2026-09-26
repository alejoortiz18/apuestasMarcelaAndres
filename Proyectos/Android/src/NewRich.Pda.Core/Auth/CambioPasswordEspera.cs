namespace NewRich.Pda.Core.Auth;

/// <summary>
/// Con conexión, cambiar la contraseña temporal no debe dejar el formulario quieto
/// ni retener el ingreso mientras se sincronizan ventas y códigos.
/// </summary>
public static class CambioPasswordEspera
{
    public const int Segundos = 8;

    public static bool EsperarSincronizacionAntesDeEntrar => false;
}
