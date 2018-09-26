# Tiger.Hal.Analyzers

## What It Is

Tiger.Hal.Analyzers is a collection of Roslyn diagnostic analyzers and code fix providers for the Tiger.Hal library.

## Why You Want It

Libraries can have restrictions that are not representable by the C# type system.
Roslyn analyzers allow library authors to provide information the the C# aompiler to signal compilation errors, warnings, and the like when these restrictions are violated, for those that can be detected at compile time.

For example, in Tiger.Hal, the selectors to the `Ignore` transformation are meaningless if they are not simple property selectors.
This transformation:

```csharp
transformationMap.Ignore(l => Method(l.Value))
```

...will error at runtime.
The type system only cares that the argument is of type `Expression<Func<T, TProperty>>`, which it is.
Tiger.Hal.Analyzers can detect this invocation at compile time and signal an error accordingly.

## Use

Using NuGet, install the package Tiger.Hal.Analyzers. It's recommended to mark "All" assets as private, as shown below.

```xml
<PackageReference Include="Tiger.Hal.Analyzers" Version="x.y.z" PrivateAssets="All" />
```

## Thank You

Seriously, though. Thank you for using this software. The author hopes it performs admirably for you.
