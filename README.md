# [TestTools.Console](https://www.nuget.org/packages/IntelliTect.TestTools.Console/): [![NuGet](https://img.shields.io/nuget/v/IntelliTect.TestTools.Console.svg)](https://www.nuget.org/packages/IntelliTect.TestTools.Console/)

Console is a simple end-to-end test framework for .NET console applications.

Check out the package at <https://www.nuget.org/packages/IntelliTect.TestTools.Console>

This currently has non-optimal nomenclature and is not guaranteed to be efficient, but it appears to work.

## Usage

### Basic Usage

The view variable contains a sample view to test for. Within it, the `<<` and `>>` symbols indicate that the inner content is entered into the console by the user -- including the newline, as they would press Enter.

```csharp
string view =
@"Please enter something: <<Something
>>You said 'Something'.";

IntelliTect.TestTools.Console.ConsoleAssert.Expect(view, () => { MyMethod() } );
```

### Wildcard Patterns

 Performs a unit test on a console-based method. A "view" of what a user would see in their console is provided as a string, where their input (including line-breaks) is surrounded by double less-than/greater-than signs, like so: "Input please: &lt;&lt;Input&gt;&gt;".

```csharp
string expected = $@"(abstract, 1)
(abstract, 2)
(abstract, 3)*
(add\*, 1)";

IntelliTect.TestTools.Console.ConsoleAssert.ExpectLike(expected, () =>
{
    Program.Main();
});
```

### Normalize Line Endings

Normalizes all line endings of the input string into the current Environment newline string.

```csharp
string expected = $@"test123
and2*
and this is my *
expected message";

expected = IntelliTect.TestTools.Console.ConsoleAssert.NormalizeLineEndings(expected);

IntelliTect.TestTools.Console.ConsoleAssert.ExpectLike(expected, () =>
{
    Program.Main();
});
```

#### Execute Process and TestGeneration

Performs a unit test on a console-based executable. what a user would see in their console is provided as a string, where their input (including line-breaks) is surrounded by double less-than/greater-than signs, like so: "Input please: &lt;&lt;Input&gt;&gt;".

```csharp
        string expected = $@"*
Pinging * ?::1? with 32 bytes of data:
Reply from ::1: time*";
        string pingArgs = "-c 4 localhost";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            pingArgs = pingArgs.Replace("-c ", "-n ");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            expected = $@"PING *(* (::1)) 56 data bytes
64 bytes from * (::1): icmp_seq=1 ttl=64 time=* ms*";
        }

        ConsoleAssert.ExecuteProcess(
            expected,
            "ping", pingArgs, out string _, out _);
```

... More to come later.
