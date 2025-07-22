using System;
using System.Threading;

namespace VariableValueMonitor.Timing;

/// <summary>
/// A class providing a timer implementation using <see cref="System.Threading.Timer"/>.
/// </summary>
public class TimerProvider : ITimerProvider
{
    /// <inheritdoc cref="ITimerProvider.CreateTimer(Action, TimeSpan)"/>
    public IDisposable CreateTimer(Action callback, TimeSpan delay)
    {
        return new Timer(_ => callback(), null, delay, Timeout.InfiniteTimeSpan);
    }

    /// <inheritdoc cref="ITimerProvider.UtcNow"/>
    public DateTime UtcNow => DateTime.UtcNow;
}