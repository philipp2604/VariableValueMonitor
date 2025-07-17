using VariableValueMonitor.Enums;

namespace VariableValueMonitor.Alarms.Conditions;

/// <summary>
/// A class defining an internal monitor condition.
/// </summary>
internal class MonitorCondition
{
    /// <summary>
    /// Creates a new instance of <see cref="MonitorCondition"/>, defining an internal condition for the <see cref="VariableValueMonitor.Monitor.ValueMonitor"/>
    /// </summary>
    /// <param name="alarmType">Type of alarm to be raised once the condition is met.</param>
    /// <param name="direction"><see cref="AlarmDirection"/> of the alarm.</param>
    /// <param name="condition">Condition function validating only the current value.</param>
    /// <param name="changeCondition">Condition function validating the old and the new value.</param>
    /// <param name="message">Message to be displayed when the alarm is triggered.</param>
    /// <param name="thresholdValue">Threshold value for comparison, used for threshold-based conditions only.</param>
    public MonitorCondition(AlarmType alarmType, AlarmDirection direction, Func<object, bool> condition, Func<object, object, bool> changeCondition, string message, object? thresholdValue)
    {
        AlarmType = alarmType;
        Direction = direction;
        Condition = condition;
        ChangeCondition = changeCondition;
        Message = message;
        ThresholdValue = thresholdValue;
    }

    /// <summary>
    /// Creates a new instance of <see cref="MonitorCondition"/>, defining an internal condition for the <see cref="VariableValueMonitor.Monitor.ValueMonitor"/>
    /// </summary>
    /// <param name="alarmType">Type of alarm to be raised once the condition is met.</param>
    /// <param name="direction"><see cref="AlarmDirection"/> of the alarm.</param>
    /// <param name="condition">Condition function validating only the current value.</param>
    /// <param name="message">Message to be displayed when the alarm is triggered.</param>
    /// <param name="thresholdValue">Threshold value for comparison, used for threshold-based conditions only.</param>
    public MonitorCondition(AlarmType alarmType, AlarmDirection direction, Func<object, bool> condition, string message, object? thresholdValue)
    {
        AlarmType = alarmType;
        Direction = direction;
        Condition = condition;
        Message = message;
        ThresholdValue = thresholdValue;
    }

    /// <summary>
    /// Creates a new instance of <see cref="MonitorCondition"/>, defining an internal condition for the <see cref="VariableValueMonitor.Monitor.ValueMonitor"/>
    /// </summary>
    /// <param name="alarmType">Type of alarm to be raised once the condition is met.</param>
    /// <param name="direction"><see cref="AlarmDirection"/> of the alarm.</param>
    /// <param name="changeCondition">Condition function validating the old and the new value.</param>
    /// <param name="message">Message to be displayed when the alarm is triggered.</param>
    /// <param name="thresholdValue">Threshold value for comparison, used for threshold-based conditions only.</param>
    public MonitorCondition(AlarmType alarmType, AlarmDirection direction, Func<object, object, bool> changeCondition, string message, object? thresholdValue)
    {
        AlarmType = alarmType;
        Direction = direction;
        ChangeCondition = changeCondition;
        Message = message;
        ThresholdValue = thresholdValue;
    }

    /// <summary>
    /// Type of alarm to be raised once the condition is met.
    /// </summary>
    public AlarmType AlarmType { get; set; }

    /// <summary>
    /// <see cref="AlarmDirection"/> of the alarm.
    /// </summary>
    public AlarmDirection Direction { get; set; }

    /// <summary>
    /// Condition function validating only the current value.
    /// </summary>
    public Func<object, bool>? Condition { get; set; }

    /// <summary>
    /// Condition function validating the old and the new value.
    /// </summary>
    public Func<object, object, bool>? ChangeCondition { get; set; }

    /// <summary>
    /// Message to be displayed when the alarm is triggered.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Threshold value, defining when the alarm is raised.
    /// </summary>
    public object? ThresholdValue { get; set; }
}