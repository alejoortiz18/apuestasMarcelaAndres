namespace NewRich.Admin.Models;

public sealed class ApiEnvelope<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

public sealed class ApiCallResult<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public int StatusCode { get; init; }
    public bool Unauthorized { get; init; }

    public static ApiCallResult<T> Ok(T? data, string message, int statusCode = 200) =>
        new() { Success = true, Data = data, Message = message, StatusCode = statusCode };

    public static ApiCallResult<T> Fail(string message, int statusCode, bool unauthorized = false) =>
        new() { Success = false, Message = message, StatusCode = statusCode, Unauthorized = unauthorized };
}

public sealed class ArchivoChat
{
    public byte[] Contenido { get; init; } = [];
    public string Nombre { get; init; } = string.Empty;
    public string Tipo { get; init; } = "application/octet-stream";
}
