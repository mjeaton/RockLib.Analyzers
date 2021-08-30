# RockLib0008: No log message specified

## Cause

This rule fires when a `LogEntry` is passed to the `ILogger.Log` method without setting its `Message` property or the `Message` property is null or empty.

## Reason for rule

A log with no message does not provide any value.

## How to fix violations

To fix a violation of this rule, provide a message to the logging method or log entry.

## Examples

### Violates

```c#
public void Example1(ILogger logger)
{
    var logEntry = new LogEntry("", LogLevel.Info);
    logger.Log(logEntry);
}

public void Example2(ILogger logger)
{
    var logEntry = new LogEntry(){ Level = LogLevel.Info };
    logger.Log(logEntry);
}

public void Example3(ILogger logger)
{
    var logEntry = new LogEntry();
    logEntry.Level = LogLevel.Info;
    logger.Log(logEntry);
}

public void Example4(ILogger logger)
{
    logger.Info("");
}
```

## How to suppress violations

```c#
#pragma warning disable RockLib0008 // No log message specified
#pragma warning restore RockLib0008 // No log message specified
```
