using VariableValueMonitor.Enums;

namespace VariableValueMonitor.Alarms;

/// <summary>
/// Creates a new instance of <see cref="ActiveAlarm"/>, representing an active alarm.
/// </summary>
/// <param name="variableId">Unique id of the variable for which the alarm was raised.</param>
/// <param name="alarmType"><see cref="Enums.AlarmType"/> of the alarm.</param>
/// <param name="direction"><see cref="Enums.AlarmDirection"/> of the alarm.</param>
/// <param name="message">Message to be displayed when the alarm is triggered.</param>
/// <param name="timestamp">Timestamp of the alarm.</param>
/// <param name="currentValue">Current value of the monitored variable.</param>
/// <param name="previousValue">Last value of the monitored variable.</param>
/// <param name="thresholdValue">Threshold value to raise the alarm.</param>
public class ActiveAlarm(string variableId, AlarmType alarmType, AlarmDirection direction, string message, DateTime timestamp, object currentValue, object? previousValue, object? thresholdValue)
{
    /// <summary>
    /// Unique id of the variable for which the alarm was raised.
    /// </summary>
    public string VariableId { get; set; } = variableId;

    /// <summary>
    /// <see cref="Enums.AlarmType"/> of the alarm.
    /// </summary>
    public AlarmType AlarmType { get; set; } = alarmType;

    /// <summary>
    /// <see cref="Enums.AlarmDirection"/> of the alarm.
    /// </summary>
    public AlarmDirection Direction { get; set; } = direction;

    /// <summary>
    /// Message to be displayed when the alarm is triggered.
    /// </summary>
    public string Message { get; set; } = message;

    /// <summary>
    /// Timestamp of the alarm.
    /// </summary>
    public DateTime Timestamp { get; set; } = timestamp;

    /// <summary>
    /// Current value of the monitored variable.
    /// </summary>
    public object CurrentValue { get; set; } = currentValue;

    /// <summary>
    /// Last value of the monitored variable.
    /// </summary>
    public object? PreviousValue { get; set; } = previousValue;

    /// <summary>
    /// Threshold value to raise the alarm.
    /// </summary>
    public object? ThresholdValue { get; set; } = thresholdValue;
}