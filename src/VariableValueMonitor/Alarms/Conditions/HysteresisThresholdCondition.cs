using VariableValueMonitor.Enums;

namespace VariableValueMonitor.Alarms.Conditions;

/// <summary>
/// Represents a hysteresis threshold condition for alarms.
/// </summary>
public class HysteresisThresholdCondition
{
    /// <summary>
    /// Creates a new instance of <see cref="HysteresisThresholdCondition"/>, defining an internal condition for the <see cref="VariableValueMonitor.Monitor.ValueMonitor"/>
    /// </summary>
    /// <param name="alarmType"><see cref="Enums.AlarmType"/> of the alarm raised by the condition.</param>
    /// <param name="direction"><see cref="Enums.AlarmDirection"/> of the alarm raised by the condition.</param>
    /// <param name="triggerThreshold">The threshold value that triggers the alarm.</param>
    /// <param name="clearThreshold">The threshold value that has to be met to clear the alarm.</param>
    /// <param name="message">A message to display when the alarm is triggered.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public HysteresisThresholdCondition(AlarmType alarmType, AlarmDirection direction,
        object triggerThreshold, object clearThreshold, string message)
    {
        AlarmType = alarmType;
        Direction = direction;
        TriggerThreshold = triggerThreshold ?? throw new ArgumentNullException(nameof(triggerThreshold));
        ClearThreshold = clearThreshold ?? throw new ArgumentNullException(nameof(clearThreshold));
        Message = message ?? throw new ArgumentNullException(nameof(message));

        ValidateThresholds();
    }

    /// <summary>
    /// Gets or sets the <see cref="Enums.AlarmType"/> of the alarm raised by the condition.
    /// </summary>
    public AlarmType AlarmType { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Enums.AlarmDirection"/> of the alarm raised by the condition.
    /// </summary>
    public AlarmDirection Direction { get; set; }

    /// <summary>
    /// Gets or sets the threshold value that triggers the alarm.
    /// </summary>
    public object TriggerThreshold { get; set; }

    /// <summary>
    /// Gets or sets the threshold value that has to be met to clear the alarm.
    /// </summary>
    public object ClearThreshold { get; set; }

    /// <summary>
    /// Gets or sets the message to display when the alarm is triggered.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Validates the thresholds to ensure they are compatible with the specified direction.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    private void ValidateThresholds()
    {
        if (TriggerThreshold.GetType() != ClearThreshold.GetType())
        {
            throw new ArgumentException("Trigger and clear thresholds must be of the same type");
        }

        if (TriggerThreshold is not IComparable triggerComparable ||
            ClearThreshold is not IComparable clearComparable)
        {
            throw new ArgumentException("Thresholds must implement IComparable");
        }

        var comparison = triggerComparable.CompareTo(clearComparable);

        switch (Direction)
        {
            case AlarmDirection.UpperBound when comparison <= 0:
                throw new ArgumentException("For upper bound alarms, trigger threshold must be greater than clear threshold");
            case AlarmDirection.LowerBound when comparison >= 0:
                throw new ArgumentException("For lower bound alarms, trigger threshold must be less than clear threshold");
        }
    }
}