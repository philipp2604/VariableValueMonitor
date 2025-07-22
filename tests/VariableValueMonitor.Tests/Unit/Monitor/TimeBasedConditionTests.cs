using System;
using System.Threading;
using System.Threading.Tasks;
using VariableValueMonitor.Alarms.Conditions;
using VariableValueMonitor.Enums;
using VariableValueMonitor.Events;
using VariableValueMonitor.Monitor;
using VariableValueMonitor.Timing;

namespace VariableValueMonitor.Tests.Unit.Monitor;

public class TimeBasedConditionTests
{
    [Fact]
    public void DelayedCondition_ShouldCreateWithValidParameters()
    {
        var threshold = new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 85.0, "High temperature");
        var delay = TimeSpan.FromSeconds(5);

        var delayedCondition = new DelayedCondition<double>(threshold, delay);

        Assert.Equal(delay, delayedCondition.Delay);
        Assert.Equal(threshold.AlarmType, delayedCondition.AlarmType);
        Assert.Equal(threshold.Direction, delayedCondition.Direction);
        Assert.Equal(threshold.Message, delayedCondition.Message);
    }

    [Fact]
    public void DelayedCondition_ShouldThrowForZeroDelay()
    {
        var threshold = new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 85.0, "High temperature");

        Assert.Throws<ArgumentException>(() => new DelayedCondition<double>(threshold, TimeSpan.Zero));
    }

    [Fact]
    public void HysteresisThresholdCondition_ShouldCreateWithValidParameters()
    {
        var condition = new HysteresisThresholdCondition(
            AlarmType.Warning,
            AlarmDirection.UpperBound,
            85.0,
            75.0,
            "Temperature alarm with hysteresis");

        Assert.Equal(AlarmType.Warning, condition.AlarmType);
        Assert.Equal(AlarmDirection.UpperBound, condition.Direction);
        Assert.Equal(85.0, condition.TriggerThreshold);
        Assert.Equal(75.0, condition.ClearThreshold);
    }

    [Fact]
    public void HysteresisThresholdCondition_ShouldThrowForInvalidThresholds()
    {
        // For upper bound, trigger must be greater than clear
        Assert.Throws<ArgumentException>(() =>
            new HysteresisThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 75.0, 85.0, "Invalid"));

        // For lower bound, trigger must be less than clear
        Assert.Throws<ArgumentException>(() =>
            new HysteresisThresholdCondition(AlarmType.Warning, AlarmDirection.LowerBound, 85.0, 75.0, "Invalid"));
    }

    [Fact]
    public void ValueMonitor_DelayedCondition_ShouldDelayAlarmTriggering()
    {
        var mockTimer = new MockTimerProvider();
        var monitor = new ValueMonitor(mockTimer);

        var alarmTriggered = false;
        monitor.AlarmTriggered += (_, _) => alarmTriggered = true;

        var delayedCondition = CommonConditions.OnHighValueDelayed(85.0, TimeSpan.FromSeconds(5), "High temperature delayed");
        monitor.RegisterVariable("temp1", "Temperature Sensor", 70.0, delayedCondition);

        // Trigger condition
        monitor.NotifyValueChanged("temp1", 90.0);

        // Should not trigger immediately
        Assert.False(alarmTriggered);

        // Advance timer
        mockTimer.AdvanceTime(TimeSpan.FromSeconds(5));

        // Now should be triggered
        Assert.True(alarmTriggered);
    }

    [Fact]
    public void ValueMonitor_HysteresisCondition_ShouldUseDifferentThresholds()
    {
        var monitor = new ValueMonitor();

        var alarmTriggeredCount = 0;
        var alarmClearedCount = 0;
        monitor.AlarmTriggered += (_, _) => alarmTriggeredCount++;
        monitor.AlarmCleared += (_, _) => alarmClearedCount++;

        var hysteresisCondition = CommonConditions.OnHighValueHysteresis(85.0, 75.0, "Temperature with hysteresis");
        monitor.RegisterVariable("temp1", "Temperature Sensor", 70.0, hysteresisCondition);

        // Go above trigger threshold
        monitor.NotifyValueChanged("temp1", 90.0);
        Assert.Equal(1, alarmTriggeredCount);
        Assert.Equal(0, alarmClearedCount);

        // Drop below trigger but above clear - should stay active
        monitor.NotifyValueChanged("temp1", 80.0);
        Assert.Equal(1, alarmTriggeredCount);
        Assert.Equal(0, alarmClearedCount);

        // Drop to clear threshold - should clear
        monitor.NotifyValueChanged("temp1", 75.0);
        Assert.Equal(1, alarmTriggeredCount);
        Assert.Equal(1, alarmClearedCount);
    }

    [Fact]
    public void CommonConditions_WithDelay_ShouldCreateDelayedCondition()
    {
        var threshold = new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 85.0, "High temperature");
        var delay = TimeSpan.FromSeconds(5);

        var delayedCondition = CommonConditions.WithDelay<double>(threshold, delay);

        Assert.Equal(delay, delayedCondition.Delay);
        Assert.Equal(threshold, delayedCondition.InnerCondition);
    }
}

public class MockTimerProvider : ITimerProvider
{
    private readonly List<MockTimer> _timers = [];
    private DateTime _currentTime = DateTime.UtcNow;

    public IDisposable CreateTimer(Action callback, TimeSpan delay)
    {
        var timer = new MockTimer(callback, _currentTime.Add(delay));
        _timers.Add(timer);
        return timer;
    }

    public DateTime UtcNow => _currentTime;

    public void AdvanceTime(TimeSpan timeSpan)
    {
        _currentTime = _currentTime.Add(timeSpan);

        var expiredTimers = _timers.Where(t => !t.IsDisposed && t.ExpirationTime <= _currentTime).ToList();
        foreach (var timer in expiredTimers)
        {
            timer.Execute();
        }
    }
}

public class MockTimer(Action callback, DateTime expirationTime) : IDisposable
{
    private readonly Action _callback = callback;
    public DateTime ExpirationTime { get; } = expirationTime;
    public bool IsDisposed { get; private set; }

    public void Execute()
    {
        if (!IsDisposed)
            _callback();
    }

    public void Dispose()
    {
        IsDisposed = true;
        GC.SuppressFinalize(this);
    }
}