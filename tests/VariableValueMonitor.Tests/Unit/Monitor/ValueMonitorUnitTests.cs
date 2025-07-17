using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VariableValueMonitor.Alarms.Conditions;
using VariableValueMonitor.Enums;
using VariableValueMonitor.Events;
using VariableValueMonitor.Monitor;

namespace VariableValueMonitor.Tests.Unit.Monitor;
public class ValueMonitorUnitTests : IDisposable
{
    private readonly ValueMonitor _monitor;
    private readonly List<AlarmEventArgs> _triggeredAlarms;
    private readonly List<AlarmEventArgs> _clearedAlarms;

    public ValueMonitorUnitTests()
    {
        _monitor = new ValueMonitor();
        _triggeredAlarms = [];
        _clearedAlarms = [];

        _monitor.AlarmTriggered += (_, args) => _triggeredAlarms.Add(args);
        _monitor.AlarmCleared += (_, args) => _clearedAlarms.Add(args);
    }

    public void Dispose()
    {
        _triggeredAlarms?.Clear();
        _clearedAlarms?.Clear();
        GC.SuppressFinalize(this);
    }

    #region Threshold Tests

    [Fact]
    public void RegisterVariable_WithThresholds_ShouldRegisterSuccessfully()
    {
        // Arrange & Act
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
            new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 80.0, "High temperature"));

        // Assert
        var variables = _monitor.GetRegisteredVariables();
        Assert.Single(variables);
        Assert.Equal("temp1", variables[0].Id);
        Assert.Equal("Temperature", variables[0].Name);
        Assert.Equal(typeof(double), variables[0].ValueType);
    }

    [Fact]
    public void NotifyValueChanged_UpperBoundExceeded_ShouldTriggerAlarm()
    {
        // Arrange
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
            new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 80.0, "High temperature"));

        // Act
        _monitor.NotifyValueChanged("temp1", 85.0);

        // Assert
        Assert.Single(_triggeredAlarms);
        Assert.Equal(AlarmType.Warning, _triggeredAlarms[0].AlarmType);
        Assert.Equal(AlarmDirection.UpperBound, _triggeredAlarms[0].Direction);
        Assert.Equal("High temperature", _triggeredAlarms[0].Message);
        Assert.Equal(85.0, _triggeredAlarms[0].CurrentValue);
        Assert.Equal(20.0, _triggeredAlarms[0].PreviousValue);
    }

    [Fact]
    public void NotifyValueChanged_LowerBoundExceeded_ShouldTriggerAlarm()
    {
        // Arrange
        _monitor.RegisterVariable<double>("level1", "Tank Level", 50.0,
            new ThresholdCondition(AlarmType.Info, AlarmDirection.LowerBound, 30.0, "Low tank level"));

        // Act
        _monitor.NotifyValueChanged("level1", 25.0);

        // Assert
        Assert.Single(_triggeredAlarms);
        Assert.Equal(AlarmType.Info, _triggeredAlarms[0].AlarmType);
        Assert.Equal(AlarmDirection.LowerBound, _triggeredAlarms[0].Direction);
        Assert.Equal("Low tank level", _triggeredAlarms[0].Message);
    }

    [Fact]
    public void NotifyValueChanged_ValueReturnsToNormal_ShouldClearAlarm()
    {
        // Arrange
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
            new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 80.0, "High temperature"));

        // Act
        _monitor.NotifyValueChanged("temp1", 85.0); // Trigger alarm
        _monitor.NotifyValueChanged("temp1", 75.0); // Clear alarm

        // Assert
        Assert.Single(_triggeredAlarms);
        Assert.Single(_clearedAlarms);
        Assert.Equal(AlarmType.Warning, _clearedAlarms[0].AlarmType);
        Assert.False(_clearedAlarms[0].IsActive);
    }

    [Fact]
    public void NotifyValueChanged_MultipleThresholds_ShouldTriggerCorrectAlarms()
    {
        // Arrange
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
            new ThresholdCondition(AlarmType.Info, AlarmDirection.UpperBound, 70.0, "Warm"),
            new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 80.0, "Hot"),
            new ThresholdCondition(AlarmType.Alarm, AlarmDirection.UpperBound, 90.0, "Critical"));

        // Act
        _monitor.NotifyValueChanged("temp1", 85.0);

        // Assert
        Assert.Equal(2, _triggeredAlarms.Count); // Info and Warning should trigger
        Assert.Contains(_triggeredAlarms, a => a.AlarmType == AlarmType.Info);
        Assert.Contains(_triggeredAlarms, a => a.AlarmType == AlarmType.Warning);
        Assert.DoesNotContain(_triggeredAlarms, a => a.AlarmType == AlarmType.Alarm);
    }

    [Fact]
    public void NotifyValueChanged_AlarmTriggeredTwice_ShouldOnlyTriggerOnce()
    {
        // Arrange
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
            new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 80.0, "High temperature"));

        // Act
        _monitor.NotifyValueChanged("temp1", 85.0);
        _monitor.NotifyValueChanged("temp1", 90.0); // Should not trigger again

        // Assert
        Assert.Single(_triggeredAlarms);
    }

    #endregion

    #region Predicate Tests

    [Fact]
    public void RegisterVariable_WithPredicates_ShouldWork()
    {
        // Arrange & Act
        _monitor.RegisterVariable<bool>("notaus1", "Emergency Stop", false,
            CommonConditions.OnTrue(AlarmType.Alarm, "Emergency stop activated!"));

        // Assert
        var variables = _monitor.GetRegisteredVariables();
        Assert.Single(variables);
    }

    [Fact]
    public void NotifyValueChanged_BooleanCondition_ShouldTriggerAlarm()
    {
        // Arrange
        _monitor.RegisterVariable<bool>("notaus1", "Emergency Stop", false,
            CommonConditions.OnTrue(AlarmType.Alarm, "Emergency stop activated!"));

        // Act
        _monitor.NotifyValueChanged("notaus1", true);

        // Assert
        Assert.Single(_triggeredAlarms);
        Assert.Equal(AlarmType.Alarm, _triggeredAlarms[0].AlarmType);
        Assert.Equal("Emergency stop activated!", _triggeredAlarms[0].Message);
    }

    [Fact]
    public void NotifyValueChanged_StringCondition_ShouldTriggerAlarm()
    {
        // Arrange
        _monitor.RegisterVariable<string>("status1", "System Status", "OK",
            CommonConditions.OnStringEquals(AlarmType.Warning, "ERROR", "System error!"));

        // Act
        _monitor.NotifyValueChanged("status1", "ERROR");

        // Assert
        Assert.Single(_triggeredAlarms);
        Assert.Equal(AlarmType.Warning, _triggeredAlarms[0].AlarmType);
        Assert.Equal("System error!", _triggeredAlarms[0].Message);
    }

    [Fact]
    public void NotifyValueChanged_CustomPredicate_ShouldWork()
    {
        // Arrange
        _monitor.RegisterVariable<int>("counter1", "Counter", 0,
            new PredicateCondition<int>(AlarmType.Info, value => value % 10 == 0 && value > 0, "Counter is multiple of 10"));

        // Act
        _monitor.NotifyValueChanged("counter1", 20);

        // Assert
        Assert.Single(_triggeredAlarms);
        Assert.Equal("Counter is multiple of 10", _triggeredAlarms[0].Message);
    }

    #endregion

    #region Value Change Condition Tests

    [Fact]
    public void RegisterVariable_WithValueChangeConditions_ShouldWork()
    {
        // Arrange & Act
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
            CommonConditions.OnValueJump(AlarmType.Info, 10.0, "Temperature jump detected"));

        // Assert
        var variables = _monitor.GetRegisteredVariables();
        Assert.Single(variables);
    }

    [Fact]
    public void NotifyValueChanged_ValueJump_ShouldTriggerAlarm()
    {
        // Arrange
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
            CommonConditions.OnValueJump(AlarmType.Info, 10.0, "Temperature jump detected"));

        // Act
        _monitor.NotifyValueChanged("temp1", 35.0); // Jump of 15 degrees

        // Assert
        Assert.Single(_triggeredAlarms);
        Assert.Equal("Temperature jump detected", _triggeredAlarms[0].Message);
        Assert.Equal(35.0, _triggeredAlarms[0].CurrentValue);
        Assert.Equal(20.0, _triggeredAlarms[0].PreviousValue);
    }

    [Fact]
    public void NotifyValueChanged_SmallValueChange_ShouldNotTriggerJumpAlarm()
    {
        // Arrange
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
            CommonConditions.OnValueJump(AlarmType.Info, 10.0, "Temperature jump detected"));

        // Act
        _monitor.NotifyValueChanged("temp1", 25.0); // Jump of only 5 degrees

        // Assert
        Assert.Empty(_triggeredAlarms);
    }

    [Fact]
    public void NotifyValueChanged_ValueIncrease_ShouldTriggerAlarm()
    {
        // Arrange
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
            CommonConditions.OnValueIncrease<double>(AlarmType.Info, "Temperature increased"));

        // Act
        _monitor.NotifyValueChanged("temp1", 21.0);

        // Assert
        Assert.Single(_triggeredAlarms);
        Assert.Equal("Temperature increased", _triggeredAlarms[0].Message);
    }

    [Fact]
    public void NotifyValueChanged_ValueDecrease_ShouldTriggerAlarm()
    {
        // Arrange
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
            CommonConditions.OnValueDecrease<double>(AlarmType.Info, "Temperature decreased"));

        // Act
        _monitor.NotifyValueChanged("temp1", 19.0);

        // Assert
        Assert.Single(_triggeredAlarms);
        Assert.Equal("Temperature decreased", _triggeredAlarms[0].Message);
    }

    [Fact]
    public void NotifyValueChanged_BooleanChange_ShouldTriggerAlarm()
    {
        // Arrange
        _monitor.RegisterVariable<bool>("switch1", "Switch", false,
            CommonConditions.OnBooleanChange(AlarmType.Info, false, true, "Switch turned on"));

        // Act
        _monitor.NotifyValueChanged("switch1", true);

        // Assert
        Assert.Single(_triggeredAlarms);
        Assert.Equal("Switch turned on", _triggeredAlarms[0].Message);
    }

    #endregion

    #region Mixed Conditions Tests

    [Fact]
    public void RegisterVariable_WithMixedConditions_ShouldWork()
    {
        // Arrange & Act
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
            thresholds:
            [
                    new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 80.0, "High temperature")
            ],
            predicates:
            [
                    new PredicateCondition<double>(AlarmType.Info, temp => temp < 0, "Freezing temperature")
            ],
            changeConditions:
            [
                    CommonConditions.OnValueJump(AlarmType.Info, 15.0, "Temperature jump")
            ]);

        // Assert
        var variables = _monitor.GetRegisteredVariables();
        Assert.Single(variables);
    }

    [Fact]
    public void NotifyValueChanged_MixedConditions_ShouldTriggerMultipleAlarms()
    {
        // Arrange
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
            thresholds:
            [
                    new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 80.0, "High temperature")
            ],
            predicates: [],
            changeConditions:
            [
                    CommonConditions.OnValueJump(AlarmType.Info, 10.0, "Temperature jump")
            ]);

        // Act
        _monitor.NotifyValueChanged("temp1", 85.0); // Should trigger both threshold and jump

        // Assert
        Assert.Equal(2, _triggeredAlarms.Count);
        Assert.Contains(_triggeredAlarms, a => a.Message == "High temperature");
        Assert.Contains(_triggeredAlarms, a => a.Message == "Temperature jump");
    }

    #endregion

    #region Acknowledgment Tests

    [Fact]
    public void AcknowledgeAlarm_ShouldRemoveFromActiveAlarms()
    {
        // Arrange
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
            new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 80.0, "High temperature"));

        _monitor.NotifyValueChanged("temp1", 85.0);

        // Act
        _monitor.AcknowledgeAlarm("temp1", AlarmType.Warning, AlarmDirection.UpperBound);

        // Assert
        var activeAlarms = _monitor.GetActiveAlarms("temp1");
        Assert.Empty(activeAlarms);
    }

    [Fact]
    public void AcknowledgeAllAlarms_ShouldRemoveAllAlarmsForVariable()
    {
        // Arrange
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
            new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 80.0, "High temperature"),
            new ThresholdCondition(AlarmType.Info, AlarmDirection.UpperBound, 70.0, "Warm temperature"));

        _monitor.NotifyValueChanged("temp1", 85.0);

        // Act
        _monitor.AcknowledgeAllAlarms("temp1");

        // Assert
        var activeAlarms = _monitor.GetActiveAlarms("temp1");
        Assert.Empty(activeAlarms);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void RegisterVariable_NullId_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _monitor.RegisterVariable<double>(null!, "Temperature", 20.0));
    }

    [Fact]
    public void NotifyValueChanged_UnregisteredVariable_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _monitor.NotifyValueChanged("nonexistent", 42.0));
    }

    [Fact]
    public void RegisterVariable_WrongThresholdType_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
                new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, "not a double", "High temperature"))); // Wrong type 
    }

    #endregion

    #region Utility Tests

    [Fact]
    public void GetCurrentValue_ShouldReturnLatestValue()
    {
        // Arrange
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0);
        _monitor.NotifyValueChanged("temp1", 25.0);

        // Act
        var currentValue = _monitor.GetCurrentValue<double>("temp1");

        // Assert
        Assert.Equal(25.0, currentValue);
    }

    [Fact]
    public void GetActiveAlarms_ShouldReturnActiveAlarms()
    {
        // Arrange
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
            new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 80.0, "High temperature"));

        _monitor.NotifyValueChanged("temp1", 85.0);

        // Act
        var activeAlarms = _monitor.GetActiveAlarms();

        // Assert
        Assert.Single(activeAlarms);
        Assert.Equal("temp1", activeAlarms[0].VariableId);
        Assert.Equal(AlarmType.Warning, activeAlarms[0].AlarmType);
    }

    [Fact]
    public void UnregisterVariable_ShouldRemoveVariable()
    {
        // Arrange
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0);

        // Act
        _monitor.UnregisterVariable("temp1");

        // Assert
        var variables = _monitor.GetRegisteredVariables();
        Assert.Empty(variables);
    }

    #endregion

    #region Event Args Tests

    [Fact]
    public void NotifyValueChanged_WithEventArgs_ShouldWork()
    {
        // Arrange
        _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
            new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 80.0, "High temperature"));

        var eventArgs = new ValueChangedEventArgs("temp1", 20.0, 85.0);

        // Act
        _monitor.NotifyValueChanged(eventArgs);

        // Assert
        Assert.Single(_triggeredAlarms);
        Assert.Equal(85.0, _triggeredAlarms[0].CurrentValue);
        Assert.Equal(20.0, _triggeredAlarms[0].PreviousValue);
    }

    #endregion

    #region Enum Tests

    public enum TestState
    {
        Stopped,
        Running,
        Error,
        Maintenance
    }

    [Fact]
    public void RegisterVariable_WithEnumConditions_ShouldWork()
    {
        // Arrange & Act
        _monitor.RegisterVariable<TestState>("machine1", "Machine State", TestState.Stopped,
            CommonConditions.OnEnumEquals(AlarmType.Alarm, TestState.Error, "Machine error!"));

        // Act
        _monitor.NotifyValueChanged("machine1", TestState.Error);

        // Assert
        Assert.Single(_triggeredAlarms);
        Assert.Equal("Machine error!", _triggeredAlarms[0].Message);
    }

    [Fact]
    public void NotifyValueChanged_EnumChange_ShouldTriggerAlarm()
    {
        // Arrange
        _monitor.RegisterVariable<TestState>("machine1", "Machine State", TestState.Running,
            CommonConditions.OnEnumChange(AlarmType.Warning, TestState.Running, TestState.Error, "Machine went to error state"));

        // Act
        _monitor.NotifyValueChanged("machine1", TestState.Error);

        // Assert
        Assert.Single(_triggeredAlarms);
        Assert.Equal("Machine went to error state", _triggeredAlarms[0].Message);
    }

    #endregion

    #region Record Tests

    public record TemperatureRecord(double Value, DateTime Timestamp);

    [Fact]
    public void RegisterVariable_WithRecord_ShouldWork()
    {
        // Arrange
        var initialRecord = new TemperatureRecord(20.0, DateTime.Now);

        // Act
        _monitor.RegisterVariable<TemperatureRecord>("temp_record", "Temperature Record", initialRecord,
            new PredicateCondition<TemperatureRecord>(AlarmType.Warning, record => record.Value > 80.0, "High temperature in record"));

        var newRecord = new TemperatureRecord(85.0, DateTime.Now);
        _monitor.NotifyValueChanged("temp_record", newRecord);

        // Assert
        Assert.Single(_triggeredAlarms);
        Assert.Equal("High temperature in record", _triggeredAlarms[0].Message);
    }

    [Fact]
    public void NotifyValueChanged_RecordValueChange_ShouldTriggerAlarm()
    {
        // Arrange
        var initialRecord = new TemperatureRecord(20.0, DateTime.Now);

        _monitor.RegisterVariable<TemperatureRecord>("temp_record", "Temperature Record", initialRecord,
            new ValueChangeCondition<TemperatureRecord>(AlarmType.Info, (oldRecord, newRecord) => Math.Abs(newRecord.Value - oldRecord.Value) > 10.0, "Temperature record changed significantly"));

        // Act
        var newRecord = new TemperatureRecord(35.0, DateTime.Now);
        _monitor.NotifyValueChanged("temp_record", newRecord);

        // Assert
        Assert.Single(_triggeredAlarms);
        Assert.Equal("Temperature record changed significantly", _triggeredAlarms[0].Message);
    }

    #endregion
}
