using VariableValueMonitor.Enums;

namespace VariableValueMonitor.Alarms.Conditions;

/// <summary>
/// Creates a new instance of <see cref="PredicateCondition{T}"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="alarmType">Type of alarm to be raised once the condition is met.</param>
/// <param name="condition">Condition function for raising the alarm.</param>
/// <param name="message">Message to be displayed when the alarm is triggered.</param>
public class PredicateCondition<T>(AlarmType alarmType, Predicate<T> condition, string message)
{
    /// <summary>
    /// Type of alarm to be raised once the condition is met.
    /// </summary>
    public AlarmType AlarmType { get; set; } = alarmType;

    /// <summary>
    /// A <see cref="Predicate{T}"/> defining the alarm's condition.
    /// </summary>
    public Predicate<T> Condition { get; set; } = condition;

    /// <summary>
    /// Message to be displayed when the alarm is triggered.
    /// </summary>
    public string Message { get; set; } = message;
}