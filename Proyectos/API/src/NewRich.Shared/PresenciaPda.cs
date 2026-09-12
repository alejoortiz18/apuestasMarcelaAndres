namespace NewRich.Shared;

public static class PresenciaPda
{
    public static readonly TimeSpan Ventana = TimeSpan.FromSeconds(45);

    public static bool EstaConectado(int conexionesVivas, DateTime? ultimoPulsoUtc, DateTime utcNow)
    {
        if (conexionesVivas > 0)
        {
            return true;
        }

        return ultimoPulsoUtc is DateTime pulso && utcNow - pulso <= Ventana;
    }
}
