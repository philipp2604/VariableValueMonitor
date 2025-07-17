using VariableValueMonitor.Enums;

namespace VariableValueMonitor.Alarms.Conditions;

/// <summary>
/// Creates a new instance of <see cref="ThresholdCondition"/>.
/// </summary>
/// <param name="alarmType">Type of alarm to be raised once the threshold is met.</param>
/// <param name="direction"><see cref="AlarmDirection"/> of the alarm.</param>
/// <param name="thresholdValue">Threshold value, defining when the alarm is raised.</param>
/// <param name="message">Message to be displayed when the alarm is triggered.</param>
public class ThresholdCondition(AlarmType alarmType, AlarmDirection direction, object thresholdValue, string message)
{
    /// <summary>
    /// Type of alarm to be raised once the threshold is met.
    /// </summary>
    public AlarmType AlarmType { get; set; } = alarmType;

    /// <summary>
    /// <see cref="AlarmDirection"/> of the alarm.
    /// </summary>
    public AlarmDirection Direction { get; set; } = direction;

    /// <summary>
    /// Threshold value, defining when the alarm is raised.
    /// </summary>
    public object ThresholdValue { get; set; } = thresholdValue;

    /// <summary>
    /// Message to be displayed when the alarm is triggered.
    /// </summary>
    public string Message { get; set; } = message;
}