using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VariableValueMonitor.Alarms;
using VariableValueMonitor.Alarms.Conditions;
using VariableValueMonitor.Enums;
using VariableValueMonitor.Events;
using VariableValueMonitor.Variables;

namespace VariableValueMonitor.Monitor;
public interface IValueMonitor
{
    /// <summary>
    /// An event being fired when an alarm is triggered.
    /// </summary>
    public event EventHandler<AlarmEventArgs>? AlarmTriggered;

    /// <summary>
    /// An event being fired when an alarm is cleared.
    /// </summary>
    public event EventHandler<AlarmEventArgs>? AlarmCleared;

    /// <summary>
    /// Register a variable for monitoring using <see cref="ThresholdCondition"/>s.
    /// </summary>
    /// <typeparam name="T"><see cref="Type"/> of the variable to be monitored.</typeparam>
    /// <param name="id">Unique id of the variable to be monitored.</param>
    /// <param name="name">Name of the variable to be monitored.</param>
    /// <param name="initialValue">Initial value of the variable to be monitored.</param>
    /// <param name="thresholds"><see cref="ThresholdCondition"/>s to be monitored.</param>
    /// <exception cref="ArgumentException"></exception>
    public void RegisterVariable<T>(string id, string name, T initialValue, params ThresholdCondition[] thresholds)
        where T : IComparable<T>;

    /// <summary>
    /// Register a variable for monitoring using <see cref="PredicateCondition{T}"/>s.
    /// </summary>
    /// <typeparam name="T"><see cref="Type"/> of the variable to be monitored.</typeparam>
    /// <param name="id">Unique id of the variable to be monitored.</param>
    /// <param name="name">Name of the variable to be monitored.</param>
    /// <param name="initialValue">Initial value of the variable to be monitored.</param>
    /// <param name="predicates"><see cref="PredicateCondition{T}"/>s to be monitored.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void RegisterVariable<T>(string id, string name, T initialValue, params PredicateCondition<T>[] predicates);

    /// <summary>
    /// Registers a variable for monitoring using <see cref="ValueChangeCondition{T}"/>s.
    /// </summary>
    /// <typeparam name="T"><see cref="Type"/> of the variable to be monitored.</typeparam>
    /// <param name="id">Unique id of the variable to be monitored.</param>
    /// <param name="name">Name of the variable to be monitored.</param>
    /// <param name="initialValue">Initial value of the variable to be monitored.</param>
    /// <param name="changeConditions"><see cref="ValueChangeCondition{T}"/>s to be monitored.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void RegisterVariable<T>(string id, string name, T initialValue, params ValueChangeCondition<T>[] changeConditions);

    /// <summary>
    /// Registers a variable for monitoring using mixed conditions.
    /// </summary>
    /// <typeparam name="T"><see cref="Type"/> of the variable to be monitored.</typeparam>
    /// <param name="id">Unique id of the variable to be monitored.</param>
    /// <param name="name">Name of the variable to be monitored.</param>
    /// <param name="initialValue">Initial value of the variable to be monitored.</param>
    /// <param name="thresholds"><see cref="ThresholdCondition"/>s to be monitored.</param>
    /// <param name="predicates"><see cref="PredicateCondition{T}"/>s to be monitored.</param>
    /// <param name="changeConditions"><see cref="ValueChangeCondition{T}"/>s to be monitored.</param>
    /// <exception cref="ArgumentException"></exception>
    public void RegisterVariable<T>(string id, string name, T initialValue,
        ThresholdCondition[]? thresholds = null,
        PredicateCondition<T>[]? predicates = null,
        ValueChangeCondition<T>[]? changeConditions = null)
        where T : IComparable<T>;

    /// <summary>
    /// Notify the monitor that a variable's value changed, triggers the condition checks.
    /// </summary>
    /// <typeparam name="T"><see cref="Type"/> of the monitored variable.</typeparam>
    /// <param name="variableId">Unique id of the monitored variable.</param>
    /// <param name="newValue">New value of the monitored variable.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public void NotifyValueChanged<T>(string variableId, T newValue);

    /// <summary>
    /// Notify the monitor that a variable's value changed, triggers the condition checks.
    /// </summary>
    /// <param name="args">The <see cref="ValueChangedEventArgs"/></param>
    /// <exception cref="ArgumentException"></exception>
    public void NotifyValueChanged(ValueChangedEventArgs args);

    /// <summary>
    /// Get a variable's last saved value.
    /// </summary>
    /// <typeparam name="T"><see cref="Type"/> of the variable.</typeparam>
    /// <param name="variableId">The variable's unique id.</param>
    /// <returns>The last saved value of the monitored variable of type <typeparamref name="T"/></returns>
    public T? GetCurrentValue<T>(string variableId);

    /// <summary>
    /// Acknowledge an alarm.
    /// </summary>
    /// <param name="variableId">The variable's unique id.</param>
    /// <param name="alarmType"><see cref="AlarmType"/> of the alarm.</param>
    /// <param name="direction"><see cref="AlarmDirection"/> of the alarm.</param>
    /// <param name="conditionIndex">The condition index.</param>
    public void AcknowledgeAlarm(string variableId, AlarmType alarmType, AlarmDirection direction, int conditionIndex = 0);

    /// <summary>
    /// Acknowledge all alarms of a variable.
    /// </summary>
    /// <param name="variableId">The variable's unique id.</param>
    public void AcknowledgeAllAlarms(string variableId);

    /// <summary>
    /// Get all active alarms.
    /// </summary>
    /// <returns>A list of <see cref="ActiveAlarm"/> representing all active alarms.</returns>
    public List<ActiveAlarm> GetActiveAlarms();

    /// <summary>
    /// Get active alarms for one variable.
    /// </summary>
    /// <param name="variableId">The variable's unique id.</param>
    /// <returns>A list of <see cref="ActiveAlarm"/> representing the active alarms of the variable.</returns>
    public List<ActiveAlarm> GetActiveAlarms(string variableId);

    /// <summary>
    /// Unregister a variable from monitoring.
    /// </summary>
    /// <param name="variableId">The variable's unique id.</param>
    public void UnregisterVariable(string variableId);

    /// <summary>
    /// Gets all registered and monitored variables.
    /// </summary>
    /// <returns>A list of <see cref="VariableRegistration"/> representing all registered and monitored variables.</returns>
    public List<VariableRegistration> GetRegisteredVariables();

    /// <summary>
    /// Register a variable for monitoring using <see cref="DelayedCondition{T}"/>s.
    /// </summary>
    /// <typeparam name="T"><see cref="Type"/> of the variable to be monitored.</typeparam>
    /// <param name="id">Unique id of the variable to be monitored.</param>
    /// <param name="name">Name of the variable to be monitored.</param>
    /// <param name="initialValue">Initial value of the variable to be monitored.</param>
    /// <param name="delayedConditions"><see cref="DelayedCondition{T}"/>s to be monitored.</param>
    /// <exception cref="ArgumentException"></exception>
    public void RegisterVariable<T>(string id, string name, T initialValue, params DelayedCondition<T>[] delayedConditions)
        where T : IComparable<T>;

    /// <summary>
    /// Register a variable for monitoring using <see cref="HysteresisThresholdCondition"/>s.
    /// </summary>
    /// <typeparam name="T"><see cref="Type"/> of the variable to be monitored.</typeparam>
    /// <param name="id">Unique id of the variable to be monitored.</param>
    /// <param name="name">Name of the variable to be monitored.</param>
    /// <param name="initialValue">Initial value of the variable to be monitored.</param>
    /// <param name="hysteresisConditions"><see cref="HysteresisThresholdCondition"/>s to be monitored.</param>
    /// <exception cref="ArgumentException"></exception>
    public void RegisterVariable<T>(string id, string name, T initialValue, params HysteresisThresholdCondition[] hysteresisConditions)
        where T : IComparable<T>;
}
