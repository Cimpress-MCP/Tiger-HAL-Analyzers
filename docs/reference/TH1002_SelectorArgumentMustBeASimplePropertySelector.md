# TH1002: Selector argument must be a simple property selector

## Cause

When using the method `Ignore`, the `selector` argument is any C# construct but a lambda selecting a property on its argument.

## Rule description

Ignoring anything but a simple property selector will fail at runtime.

## How to fix violations

Remove the `Ignore` transformation.

## When to suppress warnings

No such situations are known. If this diagnostic is triggered in error, that is considered a bug against the library.

## Example of a violation

### Description

For example, if the selector is wrapped in a method call, it no longer represents a simple property selector.

### Code

```csharp
transformationMap.Ignore(l => Method(l.Link)).Ignore(l => l.AnotherLink);
```

## Example of how to fix

### Description

Remove the malformed `Ignore` transformation.

### Code

```csharp
transformationMap.Ignore(l => l.AnotherLink);
```

## Related rules

- [TH1001: Selector argument must be a simple property selector](https://github.com/Cimpress-MCP/Tiger.Hal.Analyzers/blob/master/docs/reference/TH1001_SelectorArgumentMustBeASimplePropertySelector.md)
- [TH1004: Selector argument must be a simple property selector](https://github.com/Cimpress-MCP/Tiger.Hal.Analyzers/blob/master/docs/reference/TH1004_SelectorArgumentMustBeASimplePropertySelector.md)
