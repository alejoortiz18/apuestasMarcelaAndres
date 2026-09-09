namespace NewRich.Shared.Results;

public class Result
{
    public bool IsSuccess { get; }
    public string Message { get; }
    public int StatusCode { get; }

    protected Result(bool isSuccess, string message, int statusCode)
    {
        IsSuccess = isSuccess;
        Message = message;
        StatusCode = statusCode;
    }

    public static Result Ok(string message) => new(true, message, 200);

    public static Result Fail(string message, int statusCode = 400) => new(false, message, statusCode);
}

public class Result<T> : Result
{
    public T? Data { get; }

    private Result(bool isSuccess, string message, int statusCode, T? data)
        : base(isSuccess, message, statusCode)
    {
        Data = data;
    }

    public static Result<T> Ok(T data, string message) => new(true, message, 200, data);

    public static Result<T> Created(T data, string message) => new(true, message, 201, data);

    public static new Result<T> Fail(string message, int statusCode = 400) => new(false, message, statusCode, default);
}
