# RockLib0007: Use anonymous object in logging methods

## Cause

This rule fires when a non-anonymous object is passed as the `extendedProperties` argument for all logging methods on `ILogger` as well as the constructor for `LogEntry` 

## Reason for rule

Serialization rules may differ from object to object and may not log all properties correctly.

## How to fix violations

To fix a violation of this rule, apply the appropriate code fix:

- Pass anonymous objects in all `ILogger` log methods `extendedProperties` argument.
- Pass anonymous objects in the `LogEntry` constructor's `extendedProperties` argument.

## Examples

### Violates

```c#
public void Example(ILogger logger)
{    
    // log method
    logger.DebugSanitized("message", new Thing());

    // creating a LogEntry
    var entry = new LogEntry("message", extendedProperties: new Thing());
}
```

### Does Not Violate
```c#
public void Example(ILogger logger)
{    
    // Writing an Info log
    // In this example, Thing() must be decorated with the SafeToLog attribute
    logger.InfoSanitized("message", new { Value = new Thing() });

    // Creating a LogEntry
    var entry = new LogEntry("message", extendedProperties: new { Value = "someValue" };
    
    // Creating a LogEntry with an object as extendedProperties
    // Remember, the object being logged, must have the SafeToLog attribute
    var entry2 = new LogEntry("message");
    entry2.SetSanitizedExtendedProperty("Value", new Thing());
}
```

## How to suppress violations

```c#
#pragma warning disable RockLib0007 // Use anonymous object in logging methods
#pragma warning restore RockLib0007 // Use anonymous object in logging methods
```
