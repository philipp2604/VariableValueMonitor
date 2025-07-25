﻿using VariableValueMonitor.Enums;

namespace VariableValueMonitor.Alarms.Conditions;

public static class CommonConditions
{
    #region Boolean conditions

    public static PredicateCondition<bool> OnTrue(AlarmType alarmType, string message) =>
        new(
            alarmType,
            value => value,
            message
        );

    public static PredicateCondition<bool> OnFalse(AlarmType alarmType, string message) =>
        new(
            alarmType,
            value => !value,
            message
        );

    public static ValueChangeCondition<bool> OnBooleanChange(AlarmType alarmType, bool from, bool to, string message) =>
        new(
            alarmType,
            (oldValue, newValue) => oldValue == from && newValue == to,
            message
        );

    #endregion Boolean conditions

    #region String conditions

    public static PredicateCondition<string> OnStringEquals(AlarmType alarmType, string targetValue, string message) =>
        new(
            alarmType,
            value => string.Equals(value, targetValue, StringComparison.OrdinalIgnoreCase),
            message
        );

    public static PredicateCondition<string> OnStringContains(AlarmType alarmType, string containsValue, string message) =>
        new(
            alarmType,
            value => value?.Contains(containsValue) == true,
            message
        );

    public static PredicateCondition<string> OnStringEmpty(AlarmType alarmType, string message) =>
        new(
            alarmType,
            value => string.IsNullOrEmpty(value),
            message
        );

    #endregion String conditions

    #region Numeric conditions

    public static ValueChangeCondition<double> OnValueJump(AlarmType alarmType, double threshold, string message) =>
        new(
            alarmType,
            (oldValue, newValue) => Math.Abs(newValue - oldValue) > threshold,
            message
        );

    public static ValueChangeCondition<T> OnValueIncrease<T>(AlarmType alarmType, string message) where T : IComparable<T> =>
        new(
            alarmType,
            (oldValue, newValue) => newValue.CompareTo(oldValue) > 0,
            message
        );

    public static ValueChangeCondition<T> OnValueDecrease<T>(AlarmType alarmType, string message) where T : IComparable<T> =>
        new(
            alarmType,
            (oldValue, newValue) => newValue.CompareTo(oldValue) < 0,
            message
        );

    #endregion Numeric conditions

    #region Enum conditions

    public static PredicateCondition<T> OnEnumEquals<T>(AlarmType alarmType, T targetValue, string message) where T : Enum =>
        new(
            alarmType,
            value => value.Equals(targetValue),
            message
        );

    public static ValueChangeCondition<T> OnEnumChange<T>(AlarmType alarmType, T from, T to, string message) where T : Enum =>
        new(
            alarmType,
            (oldValue, newValue) => oldValue.Equals(from) && newValue.Equals(to),
            message
        );

    #endregion Enum conditions

    #region Delayed conditions

    public static DelayedCondition<T> WithDelay<T>(ThresholdCondition condition, TimeSpan delay)
        where T : IComparable<T> =>
        new(condition, delay);

    public static DelayedCondition<T> WithDelay<T>(PredicateCondition<T> condition, TimeSpan delay) =>
        new(condition, delay);

    public static DelayedCondition<T> WithDelay<T>(ValueChangeCondition<T> condition, TimeSpan delay) =>
        new(condition, delay);

    public static DelayedCondition<double> OnHighValueDelayed(double threshold, TimeSpan delay, string message) =>
        WithDelay<double>(
            new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, threshold, message),
            delay);

    public static DelayedCondition<double> OnLowValueDelayed(double threshold, TimeSpan delay, string message) =>
        WithDelay<double>(
            new ThresholdCondition(AlarmType.Warning, AlarmDirection.LowerBound, threshold, message),
            delay);

    public static DelayedCondition<bool> OnTrueDelayed(TimeSpan delay, AlarmType alarmType, string message) =>
        WithDelay(OnTrue(alarmType, message), delay);

    public static DelayedCondition<bool> OnFalseDelayed(TimeSpan delay, AlarmType alarmType, string message) =>
        WithDelay(OnTrue(alarmType, message), delay);

    #endregion Delayed conditions

    #region Hysteresis conditions

    public static HysteresisThresholdCondition HysteresisThreshold(AlarmType alarmType, AlarmDirection direction,
        double triggerThreshold, double clearThreshold, string message) =>
        new(alarmType, direction, triggerThreshold, clearThreshold, message);

    public static HysteresisThresholdCondition OnHighValueHysteresis(double triggerValue, double clearTemp, string message) =>
        HysteresisThreshold(AlarmType.Warning, AlarmDirection.UpperBound, triggerValue, clearTemp, message);

    public static HysteresisThresholdCondition OnLowValueHysteresis(double triggerValue, double clearTemp, string message) =>
        HysteresisThreshold(AlarmType.Warning, AlarmDirection.LowerBound, triggerValue, clearTemp, message);

    #endregion Hysteresis conditions
}