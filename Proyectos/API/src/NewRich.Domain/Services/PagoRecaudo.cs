namespace NewRich.Domain.Services;

public readonly record struct EvaluacionPago(bool Aceptado, bool Reintento, decimal SaldoRestante);

public static class PagoRecaudo
{
    public static EvaluacionPago Evaluar(decimal pendiente, decimal valor, bool claveYaRegistrada)
    {
        if (claveYaRegistrada)
        {
            return new EvaluacionPago(true, true, pendiente);
        }

        if (valor <= 0m || valor > pendiente)
        {
            return new EvaluacionPago(false, false, pendiente);
        }

        return new EvaluacionPago(true, false, pendiente - valor);
    }
}
