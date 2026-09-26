namespace NewRich.Pda.Core.Api;

public static class ReintentoConexionHttp
{
    public static bool DebeReintentar(Exception excepcion, CancellationToken ct) =>
        !ct.IsCancellationRequested && excepcion is HttpRequestException or IOException;
}

public static class ConexionHttpPda
{
    /// <summary>
    /// La red móvil y el balanceador de Azure cierran un socket ocioso en menos de un minuto.
    /// Si el cliente lo conserva más tiempo, la siguiente acción falla aunque Wi-Fi o datos sigan activos.
    /// </summary>
    public static readonly TimeSpan OcioMaximo = TimeSpan.FromSeconds(15);

    public static SocketsHttpHandler CrearControlador() => new()
    {
        PooledConnectionIdleTimeout = OcioMaximo,
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        ConnectTimeout = TimeSpan.FromSeconds(10),
        KeepAlivePingDelay = TimeSpan.FromSeconds(15),
        KeepAlivePingTimeout = TimeSpan.FromSeconds(5)
    };
}
