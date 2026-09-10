using NewRich.Constants.Messages;

namespace NewRich.Pda.Core.Auth;

public static class AuthPantalla
{
    public static string Mensaje(string mensajeApi)
    {
        if (mensajeApi == AuthMessages.FueraDeHorarioOperacion)
        {
            return PdaTexts.JuegosCerrados;
        }

        return mensajeApi;
    }
}
