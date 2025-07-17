using System.Collections.Concurrent;
using VariableValueMonitor.Alarms;
using VariableValueMonitor.Alarms.Conditions;
using VariableValueMonitor.Enums;
using VariableValueMonitor.Events;
using VariableValueMonitor.Variables;

namespace VariableValueMonitor.Monitor;
public class ValueMonitor
{
    private readonly ConcurrentDictionary<string, VariableRegistration> _variables = new();
    private readonly ConcurrentDictionary<string, List<MonitorCondition>> _conditions = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _activeAlarms = new();
    private readonly ConcurrentDictionary<string, ActiveAlarm> _alarmStates = new();
    private readonly ConcurrentDictionary<string, object> _lastValues = new();

    public event EventHandler<AlarmEventArgs>? AlarmTriggered;
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
        where T : IComparable<T>
    {
        var variable = new VariableRegistration(id, name, typeof(T), initialValue);
        _variables[id] = variable;
        _lastValues[id] = initialValue;

        var conditions = new List<MonitorCondition>();
        foreach (var threshold in thresholds)
        {
            if (threshold.ThresholdValue is not T)
                throw new ArgumentException($"Threshold value must be of type {typeof(T).Name}");

            var typedThreshold = (T)threshold.ThresholdValue;

            Func<object, bool> condition = threshold.Direction switch
            {
                AlarmDirection.LowerBound => value => value is T typedValue && typedValue.CompareTo(typedThreshold) < 0,
                AlarmDirection.UpperBound => value => value is T typedValue && typedValue.CompareTo(typedThreshold) > 0,
                _ => throw new ArgumentException("Invalid direction for threshold")
            };

            conditions.Add(new MonitorCondition(threshold.AlarmType, threshold.Direction, condition, threshold.Message, threshold.ThresholdValue));
        }

        _conditions[id] = conditions;
        _activeAlarms[id] = [];
    }

    /// <summary>
    /// Register a variable for monitoring using <see cref="PredicateCondition{T}"/>s.
    /// </summary>
    /// <typeparam name="T"><see cref="Type"/> of the variable to be monitored.</typeparam>
    /// <param name="id">Unique id of the variable to be monitored.</param>
    /// <param name="name">Name of the variable to be monitored.</param>
    /// <param name="initialValue">Initial value of the variable to be monitored.</param>
    /// <param name="predicates"><see cref="PredicateCondition{T}"/>s to be monitored.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void RegisterVariable<T>(string id, string name, T initialValue, params PredicateCondition<T>[] predicates)
    {
        ArgumentNullException.ThrowIfNull(initialValue, nameof(initialValue));

        var variable = new VariableRegistration(id, name, typeof(T), initialValue);
        _variables[id] = variable;
        _lastValues[id] = initialValue;

        var conditions = new List<MonitorCondition>();
        foreach (var predicate in predicates)
        {
            conditions.Add(new MonitorCondition(predicate.AlarmType, AlarmDirection.Custom, value => value is T typedValue && predicate.Condition(typedValue), predicate.Message, null));
        }

        _conditions[id] = conditions;
        _activeAlarms[id] = [];
    }

    /// <summary>
    /// Registers a variable for monitoring using <see cref="ValueChangeCondition{T}"/>s.
    /// </summary>
    /// <typeparam name="T"><see cref="Type"/> of the variable to be monitored.</typeparam>
    /// <param name="id">Unique id of the variable to be monitored.</param>
    /// <param name="name">Name of the variable to be monitored.</param>
    /// <param name="initialValue">Initial value of the variable to be monitored.</param>
    /// <param name="changeConditions"><see cref="ValueChangeCondition{T}"/>s to be monitored.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void RegisterVariable<T>(string id, string name, T initialValue, params ValueChangeCondition<T>[] changeConditions)
    {
        ArgumentNullException.ThrowIfNull(initialValue, nameof(initialValue));

        var variable = new VariableRegistration(id, name, typeof(T), initialValue);
        _variables[id] = variable;
        _lastValues[id] = initialValue;

        var conditions = new List<MonitorCondition>();
        foreach (var changeCondition in changeConditions)
        {
            conditions.Add(new MonitorCondition
            (
                changeCondition.AlarmType,
                AlarmDirection.Custom,
                (oldValue, newValue) =>
                    oldValue is T typedOld && newValue is T typedNew &&
                    changeCondition.Condition(typedOld, typedNew),
                changeCondition.Message,
                null
            ));
        }

        _conditions[id] = conditions;
        _activeAlarms[id] = [];
    }

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
        where T : IComparable<T>
    {
        var variable = new VariableRegistration(id, name, typeof(T), initialValue);
        _variables[id] = variable;
        _lastValues[id] = initialValue;

        var conditions = new List<MonitorCondition>();

        // Add thresholds
        if (thresholds != null)
        {
            foreach (var threshold in thresholds)
            {
                if (threshold.ThresholdValue is not T)
                    throw new ArgumentException($"Threshold value must be of type {typeof(T).Name}");

                var typedThreshold = (T)threshold.ThresholdValue;

                Func<object, bool> condition = threshold.Direction switch
                {
                    AlarmDirection.LowerBound => value => value is T typedValue && typedValue.CompareTo(typedThreshold) < 0,
                    AlarmDirection.UpperBound => value => value is T typedValue && typedValue.CompareTo(typedThreshold) > 0,
                    _ => throw new ArgumentException("Invalid direction for threshold")
                };

                conditions.Add(new MonitorCondition(threshold.AlarmType, threshold.Direction, condition, threshold.Message, threshold.ThresholdValue));
            }
        }

        // Add predicates
        if (predicates != null)
        {
            foreach (var predicate in predicates)
            {
                conditions.Add(new MonitorCondition
                (
                    predicate.AlarmType,
                    AlarmDirection.Custom,
                    value => value is T typedValue && predicate.Condition(typedValue),
                    predicate.Message,
                    null
                ));
            }
        }

        // Add change conditions
        if (changeConditions != null)
        {
            foreach (var changeCondition in changeConditions)
            {
                conditions.Add(new MonitorCondition
                (
                    changeCondition.AlarmType,
                    AlarmDirection.Custom,
                    (oldValue, newValue) =>
                        oldValue is T typedOld && newValue is T typedNew &&
                        changeCondition.Condition(typedOld, typedNew),
                    changeCondition.Message,
                     null
                ));
            }
        }

        _conditions[id] = conditions;
        _activeAlarms[id] = [];
    }

    /// <summary>
    /// Notify the monitor that a variable's value changed, triggers the condition checks.
    /// </summary>
    /// <typeparam name="T"><see cref="Type"/> of the monitored variable.</typeparam>
    /// <param name="variableId">Unique id of the monitored variable.</param>
    /// <param name="newValue">New value of the monitored variable.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public void NotifyValueChanged<T>(string variableId, T newValue)
    {
        ArgumentNullException.ThrowIfNull(newValue, nameof(newValue));

        if (!_variables.TryGetValue(variableId, out VariableRegistration? variable))
            throw new ArgumentException($"Variable {variableId} is not registered");

        var oldValue = _lastValues.TryGetValue(variableId, out var lastValue) ? lastValue : default(T);

        // save new value
        _lastValues[variableId] = newValue;
        variable.CurrentValue = newValue;

        // check conditions
        CheckConditions(variableId, oldValue, newValue);
    }

    /// <summary>
    /// Notify the monitor that a variable's value changed, triggers the condition checks.
    /// </summary>
    /// <param name="args">The <see cref="ValueChangedEventArgs"/></param>
    /// <exception cref="ArgumentException"></exception>
    public void NotifyValueChanged(ValueChangedEventArgs args)
    {
        if (!_variables.TryGetValue(args.VariableId, out VariableRegistration? variable))
            throw new ArgumentException($"Variable {args.VariableId} is not registered");

        // save new value
        _lastValues[args.VariableId] = args.NewValue;
        variable.CurrentValue = args.NewValue;

        // check conditions
        CheckConditions(args.VariableId, args.OldValue, args.NewValue);
    }

    /// <summary>
    /// Checks a monitored variable's conditions.
    /// </summary>
    /// <param name="variableId">The monitored variable's unique id.</param>
    /// <param name="oldValue">The monitored variable's old value.</param>
    /// <param name="newValue">The monitored variable's new value.</param>
    /// <exception cref="ArgumentNullException"></exception>
    private void CheckConditions(string variableId, object? oldValue, object newValue)
    {
        if (!_conditions.TryGetValue(variableId, out var conditions) ||
            !_variables.TryGetValue(variableId, out var variable))
        {
            return;
        }

        var activeAlarms = _activeAlarms[variableId];

        for (int i = 0; i < conditions.Count; i++)
        {
            var condition = conditions[i];
            var alarmKey = $"{condition.Direction}_{condition.AlarmType}_{i}";

            bool shouldTrigger = false;

            // Check according to condition type
            if (condition.ChangeCondition != null)
            {
                ArgumentNullException.ThrowIfNull(oldValue, nameof(oldValue));
                shouldTrigger = condition.ChangeCondition(oldValue, newValue);
            }
            else if (condition.Condition != null)
            {
                shouldTrigger = condition.Condition(newValue);
            }

            if (shouldTrigger && !activeAlarms.Contains(alarmKey))
            {
                // Fire alarm
                activeAlarms.Add(alarmKey);
                var activeAlarm = new ActiveAlarm
                (
                    variableId,
                    condition.AlarmType,
                    condition.Direction,
                    condition.Message,
                    DateTime.Now,
                    newValue,
                    oldValue,
                    condition.ThresholdValue
                );
                _alarmStates[alarmKey + "_" + variableId] = activeAlarm;

                AlarmTriggered?.Invoke(this, new AlarmEventArgs
                (
                    variableId,
                    variable.Name,
                    condition.AlarmType,
                    condition.Direction,
                    newValue,
                    oldValue,
                    condition.ThresholdValue,
                    condition.Message,
                    DateTime.Now,
                    true
                ));
            }
            else if (!shouldTrigger && activeAlarms.Contains(alarmKey))
            {
                // Deactivate alarm
                activeAlarms.Remove(alarmKey);
                var stateKey = alarmKey + "_" + variableId;
                if (_alarmStates.TryGetValue(stateKey, out var alarm))
                {
                    AlarmCleared?.Invoke(this, new AlarmEventArgs
                    (
                        variableId,
                        variable.Name,
                        condition.AlarmType,
                        condition.Direction,
                        newValue,
                        oldValue,
                        condition.ThresholdValue,
                        condition.Message,
                        DateTime.Now,
                        false
                    ));
                }
            }
        }
    }

    /// <summary>
    /// Get a variable's last saved value.
    /// </summary>
    /// <typeparam name="T"><see cref="Type"/> of the variable.</typeparam>
    /// <param name="variableId">The variable's unique id.</param>
    /// <returns>The last saved value of the monitored variable of type <typeparamref name="T"/></returns>
    public T? GetCurrentValue<T>(string variableId)
    {
        return _lastValues.TryGetValue(variableId, out var value) && value is T typedValue ? typedValue : default;
    }

    /// <summary>
    /// Acknowledge an alarm.
    /// </summary>
    /// <param name="variableId">The variable's unique id.</param>
    /// <param name="alarmType"><see cref="AlarmType"/> of the alarm.</param>
    /// <param name="direction"><see cref="AlarmDirection"/> of the alarm.</param>
    /// <param name="conditionIndex">The condition index.</param>
    public void AcknowledgeAlarm(string variableId, AlarmType alarmType, AlarmDirection direction, int conditionIndex = 0)
    {
        if (!_activeAlarms.TryGetValue(variableId, out var activeAlarms))
            return;

        string alarmKey = $"{direction}_{alarmType}_{conditionIndex}";
        activeAlarms.Remove(alarmKey);
        _alarmStates.TryRemove(alarmKey + "_" + variableId, out _);
    }

    /// <summary>
    /// Acknowledge all alarms of a variable.
    /// </summary>
    /// <param name="variableId">The variable's unique id.</param>
    public void AcknowledgeAllAlarms(string variableId)
    {
        if (_activeAlarms.TryGetValue(variableId, out var activeAlarms))
        {
            activeAlarms.Clear();

            var keysToRemove = new List<string>();
            foreach (var key in _alarmStates.Keys)
            {
                if (key.EndsWith("_" + variableId))
                    keysToRemove.Add(key);
            }

            foreach (var key in keysToRemove)
            {
                _alarmStates.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// Get all active alarms.
    /// </summary>
    /// <returns>A list of <see cref="ActiveAlarm"/> representing all active alarms.</returns>
    public List<ActiveAlarm> GetActiveAlarms()
    {
        return [.. _alarmStates.Values];
    }

    /// <summary>
    /// Get active alarms for one variable.
    /// </summary>
    /// <param name="variableId">The variable's unique id.</param>
    /// <returns>A list of <see cref="ActiveAlarm"/> representing the active alarms of the variable.</returns>
    public List<ActiveAlarm> GetActiveAlarms(string variableId)
    {
        var result = new List<ActiveAlarm>();
        foreach (var kvp in _alarmStates)
        {
            if (kvp.Value.VariableId == variableId)
                result.Add(kvp.Value);
        }
        return result;
    }

    /// <summary>
    /// Unregister a variable from monitoring.
    /// </summary>
    /// <param name="variableId">The variable's unique id.</param>
    public void UnregisterVariable(string variableId)
    {
        _variables.TryRemove(variableId, out _);
        _conditions.TryRemove(variableId, out _);
        _activeAlarms.TryRemove(variableId, out _);
        _lastValues.TryRemove(variableId, out _);

        var keysToRemove = new List<string>();
        foreach (var key in _alarmStates.Keys)
        {
            if (key.EndsWith("_" + variableId))
                keysToRemove.Add(key);
        }

        foreach (var key in keysToRemove)
        {
            _alarmStates.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Gets all registered and monitored variables.
    /// </summary>
    /// <returns>A list of <see cref="VariableRegistration"/> representing all registered and monitored variables.</returns>
    public List<VariableRegistration> GetRegisteredVariables()
    {
        return [.. _variables.Values];
    }
}