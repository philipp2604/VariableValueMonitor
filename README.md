# VariableValueMonitor 🚨
A flexible, modern .NET library for monitoring variable values and triggering alarms based on a rich set of configurable conditions. It's designed to be a lightweight, type-safe, and thread-safe core for any application that needs to react to state changes, such as in industrial automation, IoT, or system monitoring.

[![.NET 8 (LTS) Build & Test](https://github.com/philipp2604/VariableValueMonitor/actions/workflows/dotnet-8-build-and-test.yml/badge.svg)](https://github.com/philipp2604/VariableValueMonitor/actions/workflows/dotnet-8-build-and-test.yml)
[![.NET 9 (Latest) Build & Test](https://github.com/philipp2604/VariableValueMonitor/actions/workflows/dotnet-9-build-and-test.yml/badge.svg)](https://github.com/philipp2604/VariableValueMonitor/actions/workflows/dotnet-9-build-and-test.yml)
[![Language](https://img.shields.io/badge/language-C%23-blue.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![NuGet Version](https://img.shields.io/nuget/v/philipp2604.VariableValueMonitor.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/philipp2604.VariableValueMonitor)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![GitHub issues](https://img.shields.io/github/issues/philipp2604/VariableValueMonitor)](https://github.com/philipp2604/VariableValueMonitor/issues)

## ✨ Key Features

- **✅ Flexible Condition Engine**: Define alarms using five powerful, type-safe condition types:
  - **Thresholds**: Trigger alarms when a numeric value goes above or below a set point (e.g., `temperature > 80.0`).
  - **Predicates**: Use custom logic (`Predicate<T>`) for complex state validation (e.g., `status == "Error"`, `isEmergencyStop == true`).
  - **Value Changes**: React to the *dynamics* of a variable by comparing its old and new values (e.g., `newValue > oldValue`, `Math.Abs(newValue - oldValue) > 10.0`).
  - **Delayed Conditions**: Wrap any condition to only trigger alarms if the condition remains active for a specified duration (e.g., prevent nuisance alarms from brief spikes).
  - **Hysteresis Thresholds**: Use separate trigger and clear thresholds to prevent oscillating alarms (e.g., trigger at 85°C, clear at 75°C).
- **🔧 Fluent Condition Builders**: Use the static `CommonConditions` class for a clean, readable way to create common alarm logic like `OnTrue()`, `OnValueJump()`, `OnStringEquals()`, `WithDelay()`, and `OnHighValueHysteresis()`.
- **⏰ Time-Based Features**: Advanced timing capabilities including delayed conditions for filtering transient spikes and hysteresis conditions for stable alarm behavior.
- **🔔 Event-Driven Alarms**: Subscribe to `AlarmTriggered` and `AlarmCleared` events to seamlessly integrate alarm logic into your application's workflow.
- **🚀 Type-Safe & Generic**: Monitor any `IComparable` or `IEquatable` type, from primitives (`int`, `double`, `bool`) and `enums` to your own custom `record` or `class` types.
- **🎛️ Stateful Alarm Management**: The monitor tracks the state of all active alarms, preventing duplicate triggers and providing `Acknowledge` functionality.
- **⚡️ Thread-Safe by Design**: Built with `ConcurrentDictionary` to ensure that registering variables and notifying value changes is safe across multiple threads.
- **🏗️ Lightweight & Modern**: A focused library with a minimal footprint, written in modern C# with clear, testable components.

## 🏛️ Architecture

The library is designed with a clear separation of concerns, making it easy to understand and extend.

- **The Monitor (`ValueMonitor`)**: The central engine and public API. It manages variable registrations, state, timing infrastructure, and orchestrates condition checking and event firing.
- **Conditions**: Five types of declarative, type-safe "rules" that you define:
  - `ThresholdCondition`: Numeric threshold comparisons
  - `PredicateCondition`: Custom predicate logic
  - `ValueChangeCondition`: Old vs new value comparisons
  - `DelayedCondition<T>`: Wraps any condition with a time delay
  - `HysteresisThresholdCondition`: Separate trigger and clear thresholds
- **Common Conditions (`CommonConditions`)**: A static helper class that acts as a factory for creating the most frequently used conditions, including time-based helpers like `WithDelay()` and `OnHighValueHysteresis()`.
- **Timing Infrastructure (`ITimerProvider`, `TimerProvider`, `ConditionTimer`)**: Manages time-based condition logic with testable abstractions.
- **Events (`AlarmEventArgs`, `ValueChangedEventArgs`)**: The primary output mechanism. These classes carry all the context about an alarm or value change to your event handlers.
- **Data Models (`ActiveAlarm`, `VariableRegistration`)**: Internal and public records/classes that represent the state of monitored variables and active alarms.

As an end-user, you will primarily interact with the `ValueMonitor` class and define your logic using the various `Condition` types.

## 🚀 Getting Started

### Installation

VariableValueMonitor will be available on NuGet. You can install it using the .NET CLI:

```bash
dotnet add package philipp2604.VariableValueMonitor 
```

Or via the NuGet Package Manager in Visual Studio.

### Quick Start

Here's a practical example demonstrating how to monitor multiple variables with different types of conditions.

```csharp
using VariableValueMonitor.Alarms.Conditions;
using VariableValueMonitor.Enums;
using VariableValueMonitor.Events;
using VariableValueMonitor.Monitor;

// 1. Initialize the ValueMonitor
var monitor = new ValueMonitor();

// 2. Subscribe to the alarm events
monitor.AlarmTriggered += OnAlarmTriggered;
monitor.AlarmCleared += OnAlarmCleared;

Console.WriteLine("--- Registering Variables for Monitoring ---");

// 3. Register variables with various conditions
// A temperature sensor with a high-temperature warning
monitor.RegisterVariable<double>("temp_sensor_1", "Main Boiler Temperature", 25.0,
    new ThresholdCondition(AlarmType.Warning, AlarmDirection.UpperBound, 85.0, "High temperature detected!"));

// An emergency stop button (boolean)
monitor.RegisterVariable<bool>("emergency_stop_1", "Main Conveyor E-Stop", false,
    CommonConditions.OnTrue(AlarmType.Alarm, "Emergency stop has been activated!"));

// A pressure sensor that alarms on a sudden jump
monitor.RegisterVariable<double>("pressure_sensor_1", "Hydraulic Pressure", 120.0,
    CommonConditions.OnValueJump(AlarmType.Info, 20.0, "Rapid pressure change detected."));

// A machine state monitor using enums
monitor.RegisterVariable<MachineState>("machine_1", "CNC Machine State", MachineState.Running,
    [CommonConditions.OnEnumEquals(AlarmType.Alarm, MachineState.Error, "Machine has entered an error state!")]);
monitor.RegisterVariable<MachineState>("machine_1", "CNC Machine State", MachineState.Running,
    [CommonConditions.OnEnumChange(AlarmType.Info, MachineState.Running, MachineState.Maintenance, "Machine is now under maintenance.")]);

// A delayed condition - only triggers after temperature is high for 30 seconds
monitor.RegisterVariable<double>("temp_delayed", "Oven Temperature", 60.0,
    CommonConditions.OnHighValueDelayed(200.0, TimeSpan.FromSeconds(30), "Sustained high temperature detected!"));

// A hysteresis condition - triggers at 85°C, clears at 75°C
monitor.RegisterVariable<double>("temp_hysteresis", "Reactor Temperature", 70.0,
    CommonConditions.OnHighValueHysteresis(85.0, 75.0, "Reactor temperature alarm"));

Console.WriteLine("--- Simulating Value Changes ---\n");

// 4. Notify the monitor of new values
monitor.NotifyValueChanged("temp_sensor_1", 90.0);       // Triggers "High temperature"
monitor.NotifyValueChanged("pressure_sensor_1", 155.5);  // Triggers "Rapid pressure change"
monitor.NotifyValueChanged("emergency_stop_1", true);    // Triggers "E-Stop activated"
monitor.NotifyValueChanged("machine_1", MachineState.Error); // Triggers "Error state"

// Demonstrate hysteresis behavior
monitor.NotifyValueChanged("temp_hysteresis", 90.0);     // Triggers at 85°C threshold
monitor.NotifyValueChanged("temp_hysteresis", 80.0);     // Stays active (above 75°C clear threshold)
monitor.NotifyValueChanged("temp_hysteresis", 74.0);     // Clears at 75°C threshold

// Demonstrate delayed condition (would need to wait 30 seconds for actual trigger)
monitor.NotifyValueChanged("temp_delayed", 220.0);       // Starts delay timer, no immediate alarm

monitor.NotifyValueChanged("temp_sensor_1", 80.0);       // Clears the temperature alarm

Console.WriteLine($"\n--- Final State ---");
Console.WriteLine($"There are {monitor.GetActiveAlarms().Count} active alarms.");

// Clean up event handlers
monitor.AlarmTriggered -= OnAlarmTriggered;
monitor.AlarmCleared -= OnAlarmCleared;


// --- Event Handlers ---

void OnAlarmTriggered(object? sender, AlarmEventArgs e)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ALARM TRIGGERED for '{e.VariableName}' ({e.VariableId})");
    Console.WriteLine($"   - Type: {e.AlarmType}, Message: {e.Message}");
    Console.WriteLine($"   - Value changed from '{e.PreviousValue}' to '{e.CurrentValue}'");
    Console.ResetColor();
}

void OnAlarmCleared(object? sender, AlarmEventArgs e)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"ALARM CLEARED for '{e.VariableName}' ({e.VariableId})");
    Console.WriteLine($"   - Message: {e.Message}");
    Console.WriteLine($"   - Current value is now '{e.CurrentValue}'");
    Console.ResetColor();
}

public enum MachineState { Stopped, Running, Maintenance, Error }

```

## ⏰ Time-Based Features

### Delayed Conditions

Delayed conditions wrap existing conditions and only trigger alarms after the condition has been active for a specified duration. This prevents nuisance alarms from brief spikes or transient conditions.

```csharp
// Only trigger after temperature exceeds 85°C for 5 seconds
var delayedCondition = CommonConditions.OnHighValueDelayed(85.0, TimeSpan.FromSeconds(5), "Sustained high temperature");
monitor.RegisterVariable("temp_sensor", "Temperature", 70.0, delayedCondition);

// You can wrap any condition type with a delay
var delayedPredicate = CommonConditions.WithDelay(
    CommonConditions.OnTrue(AlarmType.Alarm, "Emergency stop activated"),
    TimeSpan.FromSeconds(2));
monitor.RegisterVariable("e_stop", "Emergency Stop", false, delayedPredicate);
```

### Hysteresis Thresholds

Hysteresis conditions use separate trigger and clear thresholds to prevent oscillating alarms around a single threshold value.

```csharp
// Trigger alarm at 85°C, but don't clear until temperature drops to 75°C
var hysteresisCondition = CommonConditions.OnHighValueHysteresis(85.0, 75.0, "High temperature with hysteresis");
monitor.RegisterVariable("temp_sensor", "Temperature", 70.0, hysteresisCondition);

// Sequence of events:
monitor.NotifyValueChanged("temp_sensor", 90.0);  // Alarm triggered (>= 85°C)
monitor.NotifyValueChanged("temp_sensor", 82.0);  // Alarm stays active (> 75°C clear threshold)
monitor.NotifyValueChanged("temp_sensor", 74.0);  // Alarm cleared (<= 75°C)
```

### Custom Timer Provider

For testing or specialized timing needs, you can provide your own timer implementation:

```csharp
// Use custom timer provider (useful for unit testing)
var customTimerProvider = new MyCustomTimerProvider();
var monitor = new ValueMonitor(customTimerProvider);
```

## 📖 Documentation
- **[ValueMonitor API Reference](./src/VariableValueMonitor/Monitor/IValueMonitor.cs)**: The `ValueMonitor` interface is the primary entry point for all operations. Its public methods define the library's capabilities.
- **[Common Conditions Reference](./src/VariableValueMonitor/Alarms/Conditions/CommonConditions.cs)**: Explore this static class to see the available pre-built condition helpers, including time-based features.
- **[Time-Based Conditions](./tests/VariableValueMonitor.Tests/Unit/Monitor/TimeBasedConditionTests.cs)**: Examples and tests for delayed conditions and hysteresis thresholds.
- **[Unit Tests](./tests/VariableValueMonitor.Tests/Unit/Monitor/ValueMonitorUnitTests.cs)**: The unit tests provide comprehensive, focused examples for every feature and condition type.
- **[Integration Tests](./tests/VariableValueMonitor.Tests/Integration/Monitor/ValueMonitorIntegrationTests.cs)**: These tests showcase how to handle more complex, multi-alarm scenarios.

## 🤝 Contributing

Contributions are welcome! Whether it's bug reports, feature requests, or pull requests, your help is appreciated.

1.  **Fork** the repository.
2.  Create a new **branch** for your feature or bug fix.
3.  Make your changes.
4.  Add or update **unit/integration tests** to cover your changes.
5.  Submit a **Pull Request** with a clear description of your changes.

Please open an issue first to discuss any major changes.

## ⚖️ License

This project is licensed under the **MIT License**. This is a permissive license that allows for reuse in both open-source and proprietary software. You are free to use, modify, and distribute this library as you see fit. See the [LICENSE](./LICENSE.txt) file for full details.
