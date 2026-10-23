namespace NewRich.Pda.Core;

public static class JuegoNumerosManualTexts
{
    public const string Titulo = "Combo";
    public const string BotonTipo = "COMBO";
    public const string BotonTipoAyuda = "Varios números en una o varias loterías con un solo valor total.";
    public const string Loterias = "Loterías";
    public const string LoteriasAyuda = "Seleccione una o varias loterías disponibles hoy.";
    public const string ValorJugado = "Valor jugado";
    public const string ValorJugadoPlaceholder = "$ 0";
    public const string ValorJugadoAyuda = "Es el total de la jugada. Se reparte entre todos los números y loterías.";
    public const string NumeroAJugar = "Número a jugar";
    public const string NumerosAJugar = "Números a jugar";
    public const string NumeroPlaceholder = "1234";
    public const string AgregarLineaAccesible = "Agregar número";
    public const string QuitarLineaAccesible = "Quitar número";
    public const string Calculo = "Cálculo de la jugada";
    public const string TotalApostado = "Total apostado";
    public const string CantidadNumeros = "Cantidad de números ingresados";
    public const string CantidadLoterias = "Cantidad de loterías seleccionadas";
    public const string CantidadApuestas = "Cantidad de apuestas individuales";
    public const string ValorPorApuesta = "Valor por apuesta individual";
    public const string FechaHora = "Fecha y hora del juego";
    public const string VerResumen = "Ver resumen";
    public const string Resumen = "Resumen de la jugada";
    public const string ResumenAyuda = "Revise los datos antes de registrar la jugada.";
    public const string Registrar = "Registrar jugada";
    public const string Corregir = "Corregir";
    public const string Cancelar = "Cancelar jugada";
    public const string CancelarConfirma = "Se perderán los datos ingresados en esta jugada.";
    public const string MuchaSuerte = "¡Mucha suerte!";
    public const string Registrando = "Registrando jugada...";
    public const string Registrada = "Jugada registrada";
    public const string Imprimir = "Imprimir de nuevo";
    public const string Finalizar = "Finalizar";
    public const string SinLoterias = "No hay loterías disponibles para jugar en este momento.";
    public const string Cierra = "Cierra";
    public const string RequiereConexion = "Combo solo se registra con conexión al servidor.";
    public const string ConexionNoDisponible = "Conexión no disponible";
    public const string SinConexionRegistrar = "No hay conexión con el servidor. El combo no se puede jugar sin conexión. La jugada sigue en pantalla: intente registrarla de nuevo cuando vuelva la conexión.";

    public static string Sobrante(int apuestas) => apuestas == 1
        ? "La primera apuesta lleva $1 más para completar el total."
        : $"Las primeras {apuestas} apuestas llevan $1 más para completar el total.";
}
