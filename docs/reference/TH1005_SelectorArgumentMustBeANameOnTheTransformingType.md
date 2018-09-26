# TH1005: Selector argument must be a name on the transforming type

## Cause

When using the `string` flavor of the method `Ignore`, the `selector` argument is not a `nameof` expression naming a member of the transforming type.

## Rule description

Ignoring anything but a member of the transforming type incurs a runtime cost for no benefit.

## How to fix violations

If the value represents a member of the transforming type, change the argument to provide `nameof` expression selecting the member.
If the value does not represent a member of the transforming type, remove the argument altogether.

## When to suppress warnings

Ideally never. However, if the value _does_ represent a member of the transforming type and a `nameof` expression cannot be used for some reason, then the warning should be suppressed.

## Example of a violation

### Description

For example, if a literal string is provided.

### Code

```csharp
transformationMap.Ignore("Link");
```

## Example of how to fix

### Description

To continue ignoring the `Link` property, use a `nameof` expression.

### Code

```csharp
transformationMap.Ignore(nameof(Linker.Link));
```

## Related rules

- [TH1002: Selector argument must be a simple property selector](https://github.com/Cimpress-MCP/Tiger.Hal.Analyzers/blob/master/docs/reference/TH1002_SelectorArgumentMustBeASimplePropertySelector.md)
