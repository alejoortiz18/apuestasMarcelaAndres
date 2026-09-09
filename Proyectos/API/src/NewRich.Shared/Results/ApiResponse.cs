namespace NewRich.Shared.Results;

public class ApiResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? Data { get; init; }

    public static ApiResponse From(Result result) => new()
    {
        Success = result.IsSuccess,
        Message = result.Message
    };

    public static ApiResponse From<T>(Result<T> result) => new()
    {
        Success = result.IsSuccess,
        Message = result.Message,
        Data = result.Data
    };
}
