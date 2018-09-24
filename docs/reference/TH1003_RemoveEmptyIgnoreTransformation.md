# TH1003: Remove empty ignore transformation

## Cause

When using the method `Ignore`, no selectors are provided.

## Rule description

Because the method `Ignore` accepts a `params` array, an empty argument list corresponds to an empty array,
not an overload with no parameters.

## How to fix violations

Remove the empty `Ignore` transformation.

## When to suppress warnings

No such situations are known. If this diagnostic is triggered in error, that is considered a bug against the library.

## Example of a violation

### Description

For example, if the selector is wrapped in a method call, it no longer represents a simple property selector.

### Code

```csharp
transformationMap.Link("relation", l => l.Link).Ignore();
```

## Example of how to fix

### Description

Remove the empty `Ignore` transformation.

### Code

```csharp
transformationMap.Link("relation", l => l.Link);
```
