using System;
using VariableValueMonitor.Enums;

namespace VariableValueMonitor.Alarms.Conditions;

/// <summary>
/// Represents a condition that triggers an alarm only after a specified delay has elapsed.
/// </summary>
/// <typeparam name="T">The type of the value being monitored by the underlying condition, if applicable.</typeparam>
public class DelayedCondition<T>
{
    /// <summary>
    /// Creates a new instance of <see cref="DelayedCondition{T}"/> with a specified delay and <see cref="ThresholdCondition"/>.
    /// </summary>
    /// <param name="condition">The <see cref="ThresholdCondition"/> for raising the alarm.</param>
    /// <param name="delay">The delay after which the alarm is raised.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public DelayedCondition(ThresholdCondition condition, TimeSpan delay)
    {
        if (delay <= TimeSpan.Zero)
        {
            throw new ArgumentException("Delay must be positive", nameof(delay));
        }

        InnerCondition = condition ?? throw new ArgumentNullException(nameof(condition));
        Delay = delay;
        AlarmType = condition.AlarmType;
        Direction = condition.Direction;
        Message = condition.Message;
        ThresholdValue = condition.ThresholdValue;
    }

    /// <summary>
    /// Creates a new instance of <see cref="DelayedCondition{T}"/> with a specified delay and <see cref="PredicateCondition{T}"/>.
    /// </summary>
    /// <param name="condition">The <see cref="PredicateCondition{T}"/> for raising the alarm.</param>
    /// <param name="delay">The delay after which the alarm is raised.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public DelayedCondition(PredicateCondition<T> condition, TimeSpan delay)
    {
        if (delay <= TimeSpan.Zero)
        {
            throw new ArgumentException("Delay must be positive", nameof(delay));
        }

        InnerCondition = condition ?? throw new ArgumentNullException(nameof(condition));
        Delay = delay;
        AlarmType = condition.AlarmType;
        Direction = AlarmDirection.Custom;
        Message = condition.Message;
        ThresholdValue = null;
    }

    /// <summary>
    /// Creates a new instance of <see cref="DelayedCondition{T}"/> with a specified delay and <see cref="ValueChangeCondition{T}"/>.
    /// </summary>
    /// <param name="condition">The <see cref="ValueChangeCondition{T}"/> for raising the alarm.</param>
    /// <param name="delay">The delay after which the alarm is raised.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public DelayedCondition(ValueChangeCondition<T> condition, TimeSpan delay)
    {
        if (delay <= TimeSpan.Zero)
        {
            throw new ArgumentException("Delay must be positive", nameof(delay));
        }

        InnerCondition = condition ?? throw new ArgumentNullException(nameof(condition));
        Delay = delay;
        AlarmType = condition.AlarmType;
        Direction = AlarmDirection.Custom;
        Message = condition.Message;
        ThresholdValue = null;
    }

    /// <summary>
    /// Gets the inner condition that defines when the alarm is raised.
    /// </summary>
    public object InnerCondition { get; }

    /// <summary>
    /// Gets the delay after which the alarm is raised.
    /// </summary>
    public TimeSpan Delay { get; }

    /// <summary>
    /// Gets the <see cref="AlarmType"/> of the alarm to be raised once the condition is met."/>
    /// </summary>
    public AlarmType AlarmType { get; }

    /// <summary>
    /// Gets the <see cref="AlarmDirection"/> of the alarm.
    /// </summary>
    public AlarmDirection Direction { get; }

    /// <summary>
    /// Gets the message to be displayed when the alarm is triggered.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Optional threshold value, defining when the alarm is raised.
    /// </summary>
    public object? ThresholdValue { get; }
}