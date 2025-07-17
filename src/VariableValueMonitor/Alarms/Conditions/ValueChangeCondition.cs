using VariableValueMonitor.Enums;

namespace VariableValueMonitor.Alarms.Conditions;

/// <summary>
/// Creates a new instance of <see cref="ValueChangeCondition{T}"/>
/// </summary>
/// <typeparam name="T"><see cref="Type"/> of the values.</typeparam>
/// <param name="alarmType">Type of alarm to be raised once the condition is met.</param>
/// <param name="condition"> A <see cref="Func{T1, T2, TResult}"/> defining the alarm's condition.</param>
/// <param name="message">Message to be displayed when the alarm is triggered.</param>
public class ValueChangeCondition<T>(AlarmType alarmType, Func<T, T, bool> condition, string message)
{
    /// <summary>
    /// Type of alarm to be raised once the condition is met.
    /// </summary>
    public AlarmType AlarmType { get; set; } = alarmType;

    /// <summary>
    /// A <see cref="Func{T1, T2, TResult}"/> defining the alarm's condition.
    /// </summary>
    public Func<T, T, bool> Condition { get; set; } = condition;

    /// <summary>
    /// Message to be displayed when the alarm is triggered.
    /// </summary>
    public string Message { get; set; } = message;
}