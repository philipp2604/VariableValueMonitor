# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

VariableValueMonitor is a flexible .NET library for monitoring variable values and triggering alarms based on configurable conditions. It's designed for industrial automation, IoT, and system monitoring applications.

## Build Commands

### Build the solution
```bash
dotnet build --configuration Release
```

### Run tests
```bash
dotnet test --verbosity normal
```

### Run unit tests only (excluding integration tests)
```bash
dotnet test --filter "Category!=Integration"
```

### Create NuGet package
```bash
dotnet pack --configuration Release
```

### Restore dependencies
```bash
dotnet restore
```

## Architecture

The library follows a clear separation of concerns:

- **ValueMonitor** (`src/VariableValueMonitor/Monitor/ValueMonitor.cs`): Central engine implementing `IValueMonitor` interface. Manages variable registrations, state tracking, and event orchestration using thread-safe `ConcurrentDictionary`. Supports both immediate and time-based conditions.

- **Condition System**: Five types of declarative alarm rules:
  - `ThresholdCondition`: Numeric comparisons (greater/less than thresholds)
  - `PredicateCondition<T>`: Custom logic using `Predicate<T>` delegates
  - `ValueChangeCondition<T>`: Comparing old vs new values during transitions
  - `DelayedCondition<T>`: Wraps any condition to add delay before triggering
  - `HysteresisThresholdCondition`: Uses separate trigger/clear thresholds to prevent oscillation

- **Time-Based Features**:
  - **Delayed Conditions**: Only trigger alarms if condition remains active for specified duration
  - **Hysteresis Thresholds**: Different thresholds for alarm activation vs clearing (e.g., trigger at 85°C, clear at 75°C)
  - **Timing Infrastructure**: `ITimerProvider` interface for testability with real (`TimerProvider`) and mock implementations

- **CommonConditions** (`src/VariableValueMonitor/Alarms/Conditions/CommonConditions.cs`): Static factory class providing pre-built conditions like `OnTrue()`, `OnValueJump()`, `OnStringEquals()`, `OnEnumChange()`, plus new helpers like `WithDelay()` and `OnHighTemperatureHysteresis()`.

- **Event System**: `AlarmTriggered` and `AlarmCleared` events with rich context via `AlarmEventArgs`.

- **State Management**: `ActiveAlarm` records track alarm state with acknowledgment support.

## Key Design Patterns

- **Generic Type Safety**: All monitoring works with `IComparable<T>` and `IEquatable<T>` constraints
- **Thread Safety**: Built with concurrent collections for multi-threaded scenarios  
- **Event-Driven**: Reactive architecture using .NET events
- **Fluent API**: Method chaining and builder patterns via `CommonConditions`

## Target Frameworks

- .NET 8.0
- .NET 9.0

## Testing Framework

Uses xUnit with separate unit and integration test categories. Integration tests are excluded from CI unit test runs.

## Package Information

- Package ID: `philipp2604.VariableValueMonitor`
- Repository: https://github.com/philipp2604/VariableValueMonitor
- License: MIT