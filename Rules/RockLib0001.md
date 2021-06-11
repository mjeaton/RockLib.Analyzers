# RockLib0001: Use sanitizing logging method

## Cause

This rule fires when a non-sanitizing logging method is called and the value of an extended property is eligible to be sanitized. Non-eligible types are "value" types: primitives, `enum` types, `string`, `decimal`, `DateTime`, `TimeSpan`, `DateTimeOffset`, `Guid`, `Uri`, `Encoding`, and `Type`.

## Reason for rule

Using a sanitizing logging method helps to ensure that sensitive information (such as PII/PIFI) is not accidentally logged.

## How to fix violations

To fix a violation of this rule, apply the appropriate code fix:

- Replace indexer with call to SetSanitizedExtendedProperty
- Replace add method with call to SetSanitizedExtendedProperty
- Change to sanitizing logging extension method
- Replace extendedProperties parameter with call to SetSanitizedExtendedProperties method
- Change to SetSanitizedExtendedProperties

## Examples

Note that each of the examples uses a `Client` object as the value for an extended property. The actual definition of this class doesn't matter, just as long as it isn't a "value" type, [as described above](#cause). If the extended properties in the examples had only "value" types instead, none would result in a violation.

### Violates

Setting `LogEntry.ExtendedProperties` values directly:

```c#
using RockLib.Logging;

public class Test
{
    public void Set_LogEntry_ExtendedProperty(
        LogEntry logEntry, Client client1, Client client2)
    {
        logEntry.ExtendedProperties["Client1"] = client1;
        logEntry.ExtendedProperties.Add("Client2", client2);
    }
}
```

---

Calling a non-sanitizing logging extension methods (in this case, `Info`):

```c#
using RockLib.Logging;

public class Test
{
    public void Call_Logging_ExtensionMethod(
        ILogger logger, Client client)
    {
        var extendedProperties = new { Client = client };
        logger.Info("Example message", extendedProperties);
    }
}
```

---

Passing an `extendedProperties` parameter to the `LogEntry` constructor:

```c#
using RockLib.Logging;

public class Test
{
    public void Pass_ExtendedProperties_To_LogEntry_Constructor(Client client)
    {
        var extendedProperties = new { Client = client };
        var logEntry = new LogEntry("Example message", LogLevel.Info, extendedProperties);
    }
}
```

---

Calling the `LogEntry.SetExtendedProperties` method:

```c#
using RockLib.Logging;

public class Test
{
    public void Call_LogEntry_SetExtendedProperties(
        LogEntry logEntry, Client client)
    {
        var extendedProperties = new { Client = client };
        logEntry.SetExtendedProperties(extendedProperties);
    }
}
```

## How to suppress violations

```c#
#pragma warning disable RockLib0001 // Use sanitizing logging method
#pragma warning restore RockLib0001 // Use sanitizing logging method
```
