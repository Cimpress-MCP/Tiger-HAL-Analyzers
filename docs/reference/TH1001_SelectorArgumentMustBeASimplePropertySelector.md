# TH1001: Selector argument must be a simple property selector

## Cause

When using the method `LinkAndIgnore`, the `selector` argument is any C# construct but a lambda selecting a property on its argument.

## Rule description

The method `LinkAndIgnore` uses its `selector` argument equivalently to the argument to `Ignore`.
Ignoring anything but a simple property selector will fail at runtime.

## How to fix violations

If the creation of a link relation is still desired, change the method to `Link`.

## When to suppress warnings

No such situations are known. If this diagnostic is triggered in error, that is considered a bug against the library.

## Example of a violation

### Description

For example, if the selector is wrapped in a method call, it no longer represents a simple property selector.

### Code

```csharp
transformationMap.LinkAndIgnore("relation", l => Method(l.Link));
```

## Example of how to fix

### Description

To continue creating a link from the value returned by `Method`, change the transformation from `LinkAndIgnore` to `Link`.

### Code

```csharp
transformationMap.Link("relation", l => Method(l.Link));
```

## Related rules

- [TH1002: Selector argument must be a simple property selector](https://github.com/Cimpress-MCP/Tiger.Hal.Analyzers/blob/master/docs/reference/TH1002_SelectorArgumentMustBeASimplePropertySelector.md)
- [TH1004: Selector argument must be a simple property selector](https://github.com/Cimpress-MCP/Tiger.Hal.Analyzers/blob/master/docs/reference/TH1004_SelectorArgumentMustBeASimplePropertySelector.md)
