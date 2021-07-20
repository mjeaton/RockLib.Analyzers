# RockLib0000: Extended property is not marked as safe to log

## Cause

This rule fires when a sanitizing logging method is called and the value of an extended property does not have any properties marked with the [SafeToLog] attribute.

## Reason for rule

If the value of a sanitized extended property does not have any properties marked as safe to log, then no properties are recorded. Instead, the extended property value is a warning message, "All properties from the {{type}} type have been excluded from the log entry extended properties because none were decorated with the [SafeToLog] attribute".

## How to fix violations

To fix a violation of this rule, decorate one or more properties of the offending class with the [SafeToLog] attribute. Alternatively, decorate the class itself with the [SafeToLog] attribute and also decorate any properties that are *not* safe to log with the [NotSafeToLog] attribute.

## Examples

Consider the following `Examples` class. Each of its methods call one of the sanitizing logging methods and has the potential to trigger a rule violation. Whether it does or not depends on the definition of the `Client` class.

```c#
using RockLib.Logging;
using RockLib.Logging.SafeLogging;

public static class Examples
{
    public static void Example1(ILogger logger, Client client)
    {
        logger.InfoSanitized("Example message", new { Client = client });
    }

    public static void Example2(LogEntry logEntry, Client client)
    {
        logEntry.SetSanitizedExtendedProperty("Client", client);
    }

    public static void Example3(LogEntry logEntry, Client client)
    {
        logEntry.SetSanitizedExtendedProperties(new { Client = client });
    }
}
```

### Violates

When the `Client` class is defined as follows, the examples above will trigger a violation:

```c#
public class Client
{
    public string Name { get; set; }

    public string SSN { get; set; }
}
```

### Does not violate

When the `Client` class is defined as follows, the examples above will *not* trigger a violation:

```c#
public class Client
{
    [SafeToLog]
    public string Name { get; set; }

    public string SSN { get; set; }
}
```

Alternatively, the class itself can be decorated with the [SafeToLog] attribute, and any properties that are *not* safe are decorated with the [NotSafeToLog] attribute:

```c#
[SafeToLog]
public class Client
{
    public string Name { get; set; }

    [NotSafeToLog]
    public string SSN { get; set; }
}
```

If you do not own the type that needs to be marked as safe to log, you can add `SafeToLogAttribute` to properties at runtime.

```c#
SafeToLogAttribute.Decorate<Client>(client => client.Name);
```

Alternatively, decorate the class with `SafeToLogAttribute` and exclude the unsafe properties by decorating them with `NotSafeToLogAttribute`:

```c#
SafeToLogAttribute.Decorate<Client>();
NotSafeToLogAttribute.Decorate<Client>(client => client.SSN);
```

## How to suppress violations

```c#
#pragma warning disable RockLib0000 // Extended property is not marked as safe to log
#pragma warning restore RockLib0000 // Extended property is not marked as safe to log
```
