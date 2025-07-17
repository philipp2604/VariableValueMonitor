using VariableValueMonitor.Enums;

namespace VariableValueMonitor.Events;

/// <summary>
/// Creates a new instance of <see cref="AlarmEventArgs"/>, used when an alarm is triggered.
/// </summary>
/// <param name="variableId">The monitored variable's unique id.</param>
/// <param name="variableName">The monitored variable's name.</param>
/// <param name="alarmType"><see cref="Enums.AlarmType"/> of the alarm.</param>
/// <param name="direction"><see cref="Enums.AlarmDirection"/> of the alarm.</param>
/// <param name="currentValue">Current value of the monitored variable.</param>
/// <param name="previousValue">Previous value of the monitored variable.</param>
/// <param name="thresholdValue">Threshold value, defining when the alarm is raised.</param>
/// <param name="message">Message to be displayed when the alarm is triggered.</param>
/// <param name="timestamp">Timestamp of the alarm.</param>
/// <param name="isActive">State of the alarm.</param>
public class AlarmEventArgs(string variableId, string variableName, AlarmType alarmType, AlarmDirection direction, object currentValue, object? previousValue, object? thresholdValue, string message, DateTime timestamp, bool isActive)
{
    /// <summary>
    /// The monitored variable's unique id.<.
    /// </summary>
    public string VariableId { get; set; } = variableId;

    /// <summary>
    /// The monitored variable's name.
    /// </summary>
    public string VariableName { get; set; } = variableName;

    /// <summary>
    /// <see cref="Enums.AlarmType"/> of the alarm.
    /// </summary>
    public AlarmType AlarmType { get; set; } = alarmType;

    /// <summary>
    /// <see cref="Enums.AlarmDirection"/> of the alarm.
    /// </summary>
    public AlarmDirection Direction { get; set; } = direction;

    /// <summary>
    /// Current value of the monitored variable.
    /// </summary>
    public object CurrentValue { get; set; } = currentValue;

    /// <summary>
    /// Previous value of the monitored variable.
    /// </summary>
    public object? PreviousValue { get; set; } = previousValue;

    /// <summary>
    /// Threshold value, defining when the alarm is raised.
    /// </summary>
    public object? ThresholdValue { get; set; } = thresholdValue;

    /// <summary>
    /// Message to be displayed when the alarm is triggered.
    /// </summary>
    public string Message { get; set; } = message;

    /// <summary>
    /// Timestamp of the alarm.
    /// </summary>
    public DateTime Timestamp { get; set; } = timestamp;

    /// <summary>
    /// State of the alarm.
    /// </summary>
    public bool IsActive { get; set; } = isActive;
}