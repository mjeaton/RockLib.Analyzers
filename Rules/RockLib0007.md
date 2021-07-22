# RockLib0007: Unexpected extended properties object

## Cause

This rule fires when an object of an unexpected type is passed as the `extendedProperties` argument for logging methods. The expected types are anonymous objects and string dictionaries; all other types are deemed unexpected.

## Reason for rule

Extended properties are intended to be a set of key/value pairs where the values are data from the application. When an object of an unexpected type is passed to a logging method, its public properties become the key/value pairs of the extended properties, which is likely not what the developer intended.

## How to fix violations

To fix a violation of this rule, instead passing the unexpected object directly as the `extendedProperties` argument, pass a new anonymous object with a property set to the object.

## Examples

### Violates

```c#
public void Example(Foo foo)
{
    _logger.Info("Some message", foo);
}
```

### Does Not Violate

```c#
public void Example(Foo foo)
{
    _logger.Info("Some message", new { foo });
}
```

The `extendedProperties` argument can also be a string dictionary:

```c#
public void Example(Foo foo)
{
    Dictionary<string, object> extendedProperties = new() { ["foo"] = foo };
    _logger.Info("Some message", extendedProperties);
}
```

## How to suppress violations

```c#
#pragma warning disable RockLib0007 // Unexpected extended properties object
#pragma warning restore RockLib0007 // Unexpected extended properties object
```
