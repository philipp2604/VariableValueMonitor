using System;

namespace VariableValueMonitor.Timing;

/// <summary>
/// A class representing a timer that tracks the time since a condition was first met.
/// </summary>
/// <remarks>
/// Creates a new instance of <see cref="ConditionTimer"/> with a specified delay and action to execute when the delay expires.
/// </remarks>
/// <param name="timerProvider">A <see cref="ITimerProvider"/> for providing Timers.</param>
/// <param name="delay">The delay after which the alarm is raised.</param>
/// <param name="onDelayExpired">The <see cref="Action"/> to executed when the alarm is raised.</param>
internal class ConditionTimer(ITimerProvider timerProvider, TimeSpan delay, Action onDelayExpired)
{
    private readonly ITimerProvider _timerProvider = timerProvider;
    private readonly TimeSpan _delay = delay;
    private readonly Action _onDelayExpired = onDelayExpired;
    private IDisposable? _timer;
    private DateTime? _conditionFirstMetTime;

    /// <summary>
    /// Gets or sets a value indicating whether the condition has been met.
    /// </summary>
    public bool ConditionMet { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the delay has expired since the condition was first met.
    /// </summary>
    public bool DelayExpired { get; private set; }

    /// <summary>
    /// Handles the condition change event. If the condition becomes active, it starts the timer.
    /// </summary>
    /// <param name="conditionActive"></param>
    public void OnConditionChanged(bool conditionActive)
    {
        if (conditionActive && !ConditionMet)
        {
            ConditionMet = true;
            _conditionFirstMetTime = _timerProvider.UtcNow;

            _timer?.Dispose();
            _timer = _timerProvider.CreateTimer(() =>
            {
                DelayExpired = true;
                _onDelayExpired();
            }, _delay);
        }
        else if (!conditionActive && ConditionMet)
        {
            Reset();
        }
    }

    /// <summary>
    /// Resets the timer and condition state.
    /// </summary>
    public void Reset()
    {
        ConditionMet = false;
        DelayExpired = false;
        _conditionFirstMetTime = null;
        _timer?.Dispose();
        _timer = null;
    }

    /// <summary>
    /// Gets the time remaining until the delay expires, or null if the condition is not met or the delay has already expired.
    /// </summary>
    public TimeSpan? TimeRemaining
    {
        get
        {
            if (!ConditionMet || DelayExpired || _conditionFirstMetTime == null)
                return null;

            var elapsed = _timerProvider.UtcNow - _conditionFirstMetTime.Value;
            var remaining = _delay - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Disposes the timer.
    /// </summary>
    public void Dispose()
    {
        _timer?.Dispose();
    }
}