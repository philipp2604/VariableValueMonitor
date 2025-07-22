using System.Collections.Concurrent;
using VariableValueMonitor.Alarms;
using VariableValueMonitor.Alarms.Conditions;
using VariableValueMonitor.Enums;
using VariableValueMonitor.Events;
using VariableValueMonitor.Variables;
using VariableValueMonitor.Timing;

namespace VariableValueMonitor.Monitor;

/// <summary>
/// Creates a new instance of <see cref="ValueMonitor"/>, defining an internal monitor for variables.
/// </summary>
/// <param name="timerProvider">A <see cref="ITimerProvider"/> for creating Timers.</param>
public class ValueMonitor(ITimerProvider timerProvider) : IValueMonitor
{
    private readonly ConcurrentDictionary<string, VariableRegistration> _variables = new();
    private readonly ConcurrentDictionary<string, List<MonitorCondition>> _conditions = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _activeAlarms = new();
    private readonly ConcurrentDictionary<string, ActiveAlarm> _alarmStates = new();
    private readonly ConcurrentDictionary<string, object> _lastValues = new();
    private readonly ConcurrentDictionary<string, ConditionTimer> _conditionTimers = new();
    private readonly ITimerProvider _timerProvider = timerProvider ?? throw new ArgumentNullException(nameof(timerProvider));

    public ValueMonitor() : this(new TimerProvider()) { }

    /// <inheritdoc cref="IValueMonitor.AlarmTriggered"/>
    public event EventHandler<AlarmEventArgs>? AlarmTriggered;

    /// <inheritdoc cref="IValueMonitor.AlarmCleared"/>
    public event EventHandler<AlarmEventArgs>? AlarmCleared;

    /// <inheritdoc cref="IValueMonitor.RegisterVariable{T}(string, string, T, ThresholdCondition[]?)"/>
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

    /// <inheritdoc cref="IValueMonitor.RegisterVariable{T}(string, string, T, PredicateCondition{T}[]?)"/>
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

    /// <inheritdoc cref="IValueMonitor.RegisterVariable{T}(string, string, T, ValueChangeCondition{T}[]?)"/>
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

    /// <inheritdoc cref="IValueMonitor.RegisterVariable{T}(string, string, T, ThresholdCondition[]?, PredicateCondition{T}[]?, ValueChangeCondition{T}[]?)"/>
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

    /// <inheritdoc cref="IValueMonitor.NotifyValueChanged{T}(string, T)"/>
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

    /// <inheritdoc cref=IValueMonitor.NotifyValueChanged(ValueChangedEventArgs)/>
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
            var timerKey = $"{variableId}_{condition.Direction}_{condition.AlarmType}_{i}";

            bool conditionMet = false;

            // Check according to condition type
            if (condition.ChangeCondition != null)
            {
                ArgumentNullException.ThrowIfNull(oldValue, nameof(oldValue));
                conditionMet = condition.ChangeCondition(oldValue, newValue);
            }
            else if (condition.Condition != null)
            {
                conditionMet = condition.Condition(newValue);
            }

            // Handle hysteresis conditions
            if (condition.IsHysteresis)
            {
                conditionMet = HandleHysteresisCondition(condition, newValue, conditionMet);
            }

            // Handle delayed conditions
            if (condition.IsDelayed)
            {
                if (_conditionTimers.TryGetValue(timerKey, out var timer))
                {
                    timer.OnConditionChanged(conditionMet);
                    // For delayed conditions, don't trigger immediately
                    continue;
                }
            }

            // Handle immediate alarm triggering/clearing for non-delayed conditions
            if (!condition.IsDelayed)
            {
                if (conditionMet && !activeAlarms.Contains(alarmKey))
                {
                    // Fire alarm immediately
                    TriggerAlarmImmediate(variableId, variable, condition, alarmKey, newValue, oldValue);
                }
                else if (!conditionMet && activeAlarms.Contains(alarmKey))
                {
                    // Clear alarm immediately
                    ClearAlarmImmediate(variableId, variable, condition, alarmKey, newValue, oldValue);
                }
            }
        }
    }

    /// <inheritdoc cref=IValueMonitor.GetCurrentValue{T}(string)/>
    public T? GetCurrentValue<T>(string variableId)
    {
        return _lastValues.TryGetValue(variableId, out var value) && value is T typedValue ? typedValue : default;
    }

    /// <inheritdoc cref=IValueMonitor.AcknowledgeAlarm(string, AlarmType, AlarmDirection, int)/>
    public void AcknowledgeAlarm(string variableId, AlarmType alarmType, AlarmDirection direction, int conditionIndex = 0)
    {
        if (!_activeAlarms.TryGetValue(variableId, out var activeAlarms))
            return;

        string alarmKey = $"{direction}_{alarmType}_{conditionIndex}";
        activeAlarms.Remove(alarmKey);
        _alarmStates.TryRemove(alarmKey + "_" + variableId, out _);
    }

    /// <inheritdoc cref=IValueMonitor.AcknowledgeAllAlarms(string)/>
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

    /// <inheritdoc cref=IValueMonitor.GetActiveAlarms()/>
    public List<ActiveAlarm> GetActiveAlarms()
    {
        return [.. _alarmStates.Values];
    }

    /// <inheritdoc cref=IValueMonitor.GetActiveAlarms(string)/>
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

    /// <inheritdoc cref=IValueMonitor.UnregisterVariable(string)/>
    public void UnregisterVariable(string variableId)
    {
        _variables.TryRemove(variableId, out _);
        _conditions.TryRemove(variableId, out _);
        _activeAlarms.TryRemove(variableId, out _);
        _lastValues.TryRemove(variableId, out _);

        // Clean up alarm states
        var alarmStateKeysToRemove = new List<string>();
        foreach (var key in _alarmStates.Keys)
        {
            if (key.EndsWith("_" + variableId))
                alarmStateKeysToRemove.Add(key);
        }

        foreach (var key in alarmStateKeysToRemove)
        {
            _alarmStates.TryRemove(key, out _);
        }

        // Clean up condition timers
        var timerKeysToRemove = new List<string>();
        foreach (var key in _conditionTimers.Keys)
        {
            if (key.StartsWith(variableId + "_"))
                timerKeysToRemove.Add(key);
        }

        foreach (var key in timerKeysToRemove)
        {
            if (_conditionTimers.TryRemove(key, out var timer))
            {
                timer.Dispose();
            }
        }
    }

    /// <inheritdoc cref=IValueMonitor.GetRegisteredVariables/>
    public List<VariableRegistration> GetRegisteredVariables()
    {
        return [.. _variables.Values];
    }

    /// <inheritdoc cref="IValueMonitor.RegisterVariable{T}(string, string, T, DelayedCondition{T}[]?)"/>
    public void RegisterVariable<T>(string id, string name, T initialValue, params DelayedCondition<T>[] delayedConditions)
        where T : IComparable<T>
    {
        var variable = new VariableRegistration(id, name, typeof(T), initialValue);
        _variables[id] = variable;
        _lastValues[id] = initialValue;

        var conditions = new List<MonitorCondition>();
        for (int i = 0; i < delayedConditions.Length; i++)
        {
            var delayedCondition = delayedConditions[i];
            var condition = CreateMonitorConditionFromDelayed(delayedCondition);
            conditions.Add(condition);

            if (condition.IsDelayed)
            {
                var timerKey = $"{id}_{condition.Direction}_{condition.AlarmType}_{i}";
                var conditionTimer = new ConditionTimer(
                    _timerProvider,
                    condition.Delay!.Value,
                    () => TriggerDelayedAlarm(id, i, condition));
                _conditionTimers[timerKey] = conditionTimer;
            }
        }

        _conditions[id] = conditions;
        _activeAlarms[id] = [];
    }

    /// <inheritdoc cref="IValueMonitor.RegisterVariable{T}(string, string, T, HysteresisThresholdCondition[]?)"/>
    public void RegisterVariable<T>(string id, string name, T initialValue, params HysteresisThresholdCondition[] hysteresisConditions)
        where T : IComparable<T>
    {
        var variable = new VariableRegistration(id, name, typeof(T), initialValue);
        _variables[id] = variable;
        _lastValues[id] = initialValue;

        var conditions = new List<MonitorCondition>();
        foreach (var hysteresisCondition in hysteresisConditions)
        {
            if (hysteresisCondition.TriggerThreshold is not T || hysteresisCondition.ClearThreshold is not T)
                throw new ArgumentException($"Hysteresis thresholds must be of type {typeof(T).Name}");

            var condition = CreateMonitorConditionFromHysteresis<T>(hysteresisCondition);
            conditions.Add(condition);
        }

        _conditions[id] = conditions;
        _activeAlarms[id] = [];
    }

    private static MonitorCondition CreateMonitorConditionFromDelayed<T>(DelayedCondition<T> delayedCondition)
        where T : IComparable<T>
    {
        var condition = new MonitorCondition(
            delayedCondition.AlarmType,
            delayedCondition.Direction,
            CreateConditionFunc<T>(delayedCondition.InnerCondition),
            delayedCondition.Message,
            delayedCondition.ThresholdValue)
        {
            Delay = delayedCondition.Delay
        };

        if (delayedCondition.InnerCondition is ValueChangeCondition<T> changeCondition)
        {
            condition.ChangeCondition = (oldValue, newValue) =>
                oldValue is T typedOld && newValue is T typedNew &&
                changeCondition.Condition(typedOld, typedNew);
        }

        return condition;
    }

    private static MonitorCondition CreateMonitorConditionFromHysteresis<T>(HysteresisThresholdCondition hysteresisCondition)
        where T : IComparable<T>
    {
        var triggerThreshold = (T)hysteresisCondition.TriggerThreshold;
        var clearThreshold = (T)hysteresisCondition.ClearThreshold;

        Func<object, bool> condition = hysteresisCondition.Direction switch
        {
            AlarmDirection.LowerBound => value =>
                value is T typedValue && typedValue.CompareTo(triggerThreshold) < 0,
            AlarmDirection.UpperBound => value =>
                value is T typedValue && typedValue.CompareTo(triggerThreshold) > 0,
            _ => throw new ArgumentException("Invalid direction for hysteresis threshold")
        };

        return new MonitorCondition(
            hysteresisCondition.AlarmType,
            hysteresisCondition.Direction,
            condition,
            hysteresisCondition.Message,
            hysteresisCondition.TriggerThreshold)
        {
            ClearThreshold = hysteresisCondition.ClearThreshold
        };
    }

    private static Func<object, bool> CreateConditionFunc<T>(object innerCondition) where T : IComparable<T>
    {
        return innerCondition switch
        {
            ThresholdCondition thresholdCondition => CreateThresholdFunc<T>(thresholdCondition),
            PredicateCondition<T> predicateCondition => value =>
                value is T typedValue && predicateCondition.Condition(typedValue),
            _ => throw new ArgumentException("Unsupported condition type for delayed conditions")
        };
    }

    private static Func<object, bool> CreateThresholdFunc<T>(ThresholdCondition thresholdCondition) where T : IComparable<T>
    {
        var typedThreshold = (T)thresholdCondition.ThresholdValue;
        return thresholdCondition.Direction switch
        {
            AlarmDirection.LowerBound => value =>
                value is T typedValue && typedValue.CompareTo(typedThreshold) < 0,
            AlarmDirection.UpperBound => value =>
                value is T typedValue && typedValue.CompareTo(typedThreshold) > 0,
            _ => throw new ArgumentException("Invalid direction for threshold")
        };
    }

    private static bool HandleHysteresisCondition(MonitorCondition condition, object newValue, bool conditionMet)
    {
        if (condition.ClearThreshold == null)
            return conditionMet;

        // For hysteresis, we need different logic for triggering vs clearing
        if (!condition.HysteresisAlarmActive && conditionMet)
        {
            // Alarm not active, condition met -> activate
            condition.HysteresisAlarmActive = true;
            return true;
        }
        else if (condition.HysteresisAlarmActive)
        {
            // Alarm is active, check if we should clear using clear threshold
            bool shouldClear = CheckClearThreshold(condition, newValue);
            if (shouldClear)
            {
                condition.HysteresisAlarmActive = false;
                return false;
            }
            return true; // Keep alarm active
        }

        return false;
    }

    private static bool CheckClearThreshold(MonitorCondition condition, object newValue)
    {
        if (condition.ClearThreshold == null || newValue is not IComparable newComparable)
            return false;

        var clearThreshold = (IComparable)condition.ClearThreshold;
        var comparison = newComparable.CompareTo(clearThreshold);

        return condition.Direction switch
        {
            AlarmDirection.UpperBound => comparison <= 0, // Clear when value drops to or below clear threshold
            AlarmDirection.LowerBound => comparison >= 0, // Clear when value rises to or above clear threshold
            _ => false
        };
    }

    private void TriggerAlarmImmediate(string variableId, VariableRegistration variable, MonitorCondition condition, string alarmKey, object newValue, object? oldValue)
    {
        var activeAlarms = _activeAlarms[variableId];
        activeAlarms.Add(alarmKey);

        var activeAlarm = new ActiveAlarm(
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

        AlarmTriggered?.Invoke(this, new AlarmEventArgs(
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

    private void ClearAlarmImmediate(string variableId, VariableRegistration variable, MonitorCondition condition, string alarmKey, object newValue, object? oldValue)
    {
        var activeAlarms = _activeAlarms[variableId];
        activeAlarms.Remove(alarmKey);

        var stateKey = alarmKey + "_" + variableId;
        _alarmStates.TryRemove(stateKey, out _);

        AlarmCleared?.Invoke(this, new AlarmEventArgs(
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

    private void TriggerDelayedAlarm(string variableId, int conditionIndex, MonitorCondition condition)
    {
        if (!_variables.TryGetValue(variableId, out var variable))
            return;

        var alarmKey = $"{condition.Direction}_{condition.AlarmType}_{conditionIndex}";
        var activeAlarms = _activeAlarms[variableId];

        if (!activeAlarms.Contains(alarmKey))
        {
            var currentValue = _lastValues[variableId];
            TriggerAlarmImmediate(variableId, variable, condition, alarmKey, currentValue, null);
        }
    }
}