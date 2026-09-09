namespace NewRich.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
    DateTime LocalNow { get; }
}
