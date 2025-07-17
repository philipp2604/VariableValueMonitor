namespace VariableValueMonitor.Variables;

/// <summary>
/// Creates a new <see cref="VariableRegistration"/> instance.
/// </summary>
/// <param name="id">Unique id of the variable to monitor.</param>
/// <param name="name">Display name of the variable to monitor.</param>
/// <param name="valueType"><see cref="Type"/> of the variable's value.</param>
/// <param name="currentValue">Current value of the variable to monitor</param>
public class VariableRegistration(string id, string name, Type valueType, object? currentValue = null)
{
    /// <summary>
    /// Unique id of the variable to monitor.
    /// </summary>
    public string Id { get; set; } = id;

    /// <summary>
    /// Display name of the variable to monitor.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// <see cref="Type"/> of the variable's value.
    /// </summary>
    public Type ValueType { get; set; } = valueType;

    /// <summary>
    /// Current value of the variable to monitor.
    /// </summary>
    public object? CurrentValue { get; set; } = currentValue;
}