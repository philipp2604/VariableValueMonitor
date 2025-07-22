using System;

namespace VariableValueMonitor.Timing;

/// <summary>
/// An interface defining a timer provider for creating timers and accessing the current UTC time.
/// </summary>
public interface ITimerProvider
{
    /// <summary>
    /// Creates a timer that executes a callback after a specified delay.
    /// </summary>
    /// <param name="callback">The <see cref="Action"/> to be executed.</param>
    /// <param name="delay">The delay after which the action shall be executed.</param>
    /// <returns>The timer as <see cref="IDisposable"/></returns>
    IDisposable CreateTimer(Action callback, TimeSpan delay);

    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTime UtcNow { get; }
}