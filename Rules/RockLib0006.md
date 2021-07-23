# RockLib0006: Caught exception should be logged

## Cause

This rule fires when an `ILogger` logs inside a catch block, but the catch variable is not passed to the logger.

## Reason for rule

Having the full information of an exception - particularly the stack trace - is invaluable when troubleshooting an application. Therefore, it is important to include the caught exception whenever logging inside of a catch block.

## How to fix violations

To fix a violation of this rule, apply the appropriate code fix:

- Pass exception to LogEntry constructor
- Set LogEntry.Exception property
- Pass exception to logging extension method

## Examples

### Violates

```c#
try
{
    DoSomething();
}
catch
{
    _logger.Error("Something went wrong");
}
```

### Does not violate

```c#
try
{
    DoSomething();
}
catch (Exception ex)
{
    _logger.Error("Something went wrong", ex);
}
```

## How to suppress violations

```c#
#pragma warning disable RockLib0006 // Caught exception should be logged
#pragma warning restore RockLib0006 // Caught exception should be logged
```
