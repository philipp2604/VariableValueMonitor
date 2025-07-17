namespace VariableValueMonitor.Events;

/// <summary>
/// Provides data for the ValueChanged event, fired when a monitored variable's value changed.
/// </summary>
/// <typeparam name="T"><see cref="Type"/> of the variable's value.</typeparam>
/// <param name="variableId">The monitored variable's unique id.</param>
/// <param name="oldValue">The variable's old value of type <typeparamref name="T"/>.</param>
/// <param name="newValue">The variable's new value of type <typeparamref name="T"/>.</param>
public class ValueChangedEventArgs<T>(string variableId, T oldValue, T newValue) : EventArgs
{
    /// <summary>
    /// The monitored variable's unique id.
    /// </summary>
    public string VariableId { get; set; } = variableId;

    /// <summary>
    /// The variable's old value of type <typeparamref name="T"/>.
    /// </summary>
    public T OldValue { get; set; } = oldValue;

    /// <summary>
    /// The variable's new value of type <typeparamref name="T"/>.
    /// </summary>
    public T NewValue { get; set; } = newValue;

    /// <summary>
    /// Timestamp of the alarm.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}

/// <summary>
/// Provides data for the ValueChanged event, fired when a monitored variable's value changed.
/// </summary>
/// <param name="variableId">The monitored variable's unique id.</param>
/// <param name="oldValue">The variable's old value.</param>
/// <param name="newValue">The variable's new value.</param>
public class ValueChangedEventArgs(string variableId, object oldValue, object newValue) : EventArgs
{
    /// <summary>
    /// The monitored variable's unique id.
    /// </summary>
    public string VariableId { get; set; } = variableId;

    /// <summary>
    /// The variable's old value.
    /// </summary>
    public object OldValue { get; set; } = oldValue;

    /// <summary>
    /// The variable's new value.
    /// </summary>
    public object NewValue { get; set; } = newValue;

    /// <summary>
    /// Timestamp of the alarm.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}