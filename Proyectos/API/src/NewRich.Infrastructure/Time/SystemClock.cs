using NewRich.Application.Abstractions;
using NewRich.Domain.Services;

namespace NewRich.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime LocalNow => ZonaHorariaColombia.ALocal(DateTime.UtcNow);
}
