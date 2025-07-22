using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VariableValueMonitor.Alarms.Conditions;
using VariableValueMonitor.Enums;
using VariableValueMonitor.Events;
using VariableValueMonitor.Monitor;

namespace VariableValueMonitor.Tests.Integration.Monitor
{
    [Trait("Category", "Integration")]
    public class VariableValueMonitorIntegrationTests : IDisposable
    {
        private readonly ValueMonitor _monitor;
        private readonly List<AlarmEventArgs> _triggeredAlarms;
        private readonly List<AlarmEventArgs> _clearedAlarms;

        public VariableValueMonitorIntegrationTests()
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

        [Fact]
        public void ComplexScenario_MultipleAlarmsAndAcknowledgment_ShouldWork()
        {
            // Arrange
            _monitor.RegisterVariable<double>("tank1", "Tank Level", 100.0,
                thresholds:
                [
                    new ThresholdCondition(AlarmType.Info, AlarmDirection.LowerBound, 30.0, "Low level"),
                    new ThresholdCondition(AlarmType.Warning, AlarmDirection.LowerBound, 15.0, "Very low level"),
                    new ThresholdCondition(AlarmType.Alarm, AlarmDirection.LowerBound, 5.0, "Critical level")
                ],
                changeConditions:
                [
                        CommonConditions.OnValueJump(AlarmType.Warning, 20.0, "Rapid level change")
                ]);

            // Act: Simulate tank emptying
            _monitor.NotifyValueChanged("tank1", 25.0); // Should trigger Info + Jump
            _monitor.NotifyValueChanged("tank1", 4.0); // Should trigger Warning + Alarm

            // Assert
            Assert.Equal(4, _triggeredAlarms.Count);
            Assert.Contains(_triggeredAlarms, a => a.Message == "Low level");
            Assert.Contains(_triggeredAlarms, a => a.Message == "Very low level");
            Assert.Contains(_triggeredAlarms, a => a.Message == "Critical level");
            Assert.Contains(_triggeredAlarms, a => a.Message == "Rapid level change");

            // Acknowledge all alarms
            _monitor.AcknowledgeAllAlarms("tank1");
            Assert.Empty(_monitor.GetActiveAlarms("tank1"));
        }

        [Theory]
        [InlineData(85.0, true)]  // Should trigger
        [InlineData(75.0, false)] // Should not trigger
        [InlineData(80.0, false)] // Exactly at threshold - should not trigger
        public void NotifyValueChanged_ThresholdBoundaryConditions_ShouldBehaveCorrectly(double value, bool shouldTrigger)
        {
            // Arrange
            _monitor.RegisterVariable<double>("temp1", "Temperature", 20.0,
                new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 80.0, "High temperature"));

            // Act
            _monitor.NotifyValueChanged("temp1", value);

            // Assert
            if (shouldTrigger)
            {
                Assert.Single(_triggeredAlarms);
            }
            else
            {
                Assert.Empty(_triggeredAlarms);
            }
        }
    }

    #region Helpers
    public class VariableService
    {
        public event EventHandler<ValueChangedEventArgs>? VariableValueChanged;

        public string PressureName { get; } = "TankPressure";
        public string EmergencyStopName { get; } = "EmergencyStop";

        public double Pressure { get; private set; } = 1013.25;
        public bool EmergencyStop { get; private set; } = false;

        public void UpdatePressureValue(double newValue)
        {
            var oldValue = Pressure;
            Pressure = newValue;
            VariableValueChanged?.Invoke(this, new ValueChangedEventArgs(PressureName, oldValue, newValue));
        }

        public void UpdateEmergencyStopValue(bool newValue)
        {
            var oldValue = EmergencyStop;
            EmergencyStop = newValue;
            VariableValueChanged?.Invoke(this, new ValueChangedEventArgs(EmergencyStopName, oldValue, newValue));
        }
    }
    #endregion
}
