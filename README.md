[TestTools.Console](https://www.nuget.org/packages/IntelliTect.TestTools.Console/): [![NuGet](https://img.shields.io/nuget/v/IntelliTect.TestTools.Console.svg)](https://www.nuget.org/packages/IntelliTect.TestTools.Console/)
===========

Console is a simple end-to-end test framework for .NET console applications.

Check out the package at https://www.nuget.org/packages/IntelliTect.TestTools.Console

This currently has non-optimal nomenclature and is not guaranteed to be efficient, but it appears to work.

Usage
-----

To use it, use this syntax within a unit test:

```csharp
string view =
@"Please enter something: <<Something
>>You said 'Something'.";

IntelliTect.TestTools.Console.ConsoleAssert.Expect(view, () => { MyMethod() } );
```

The view variable contains a sample view to test for. Within it, the `<<` and `>>` symbols indicate that the inner content is entered into the console by the user -- including the newline, as they would press Enter.

... More to come later.
