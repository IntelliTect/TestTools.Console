using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace IntelliTect.TestTools.Console.Tests;

[TestClass]
public class ConsoleAssertTests
{
    [TestMethod]
    public void ConsoleTester_Sample_InigoMontoya()
    {
        const string view =
@"First name: <<Inigo
>>Last name: <<Montoya
>>Hello, Inigo Montoya.";

        ConsoleAssert.Expect(view,
        () =>
        {
            System.Console.Write("First name: ");
            string fname = System.Console.ReadLine();

            System.Console.Write("Last name: ");
            string lname = System.Console.ReadLine();

            System.Console.Write("Hello, {0} {1}.", fname, lname);
        });
    }

    [TestMethod]
    public async Task ConsoleTester_SampleAsync_InigoMontoya()
    {
        const string view =
@"First name: <<Inigo
>>Last name: <<Montoya
>>Hello, Inigo Montoya.";

        await ConsoleAssert.ExpectAsync(view,
        async () =>
        {
            await Task.Yield();
            System.Console.Write("First name: ");
            string fname = System.Console.ReadLine();

            System.Console.Write("Last name: ");
            string lname = System.Console.ReadLine();

            System.Console.Write("Hello, {0} {1}.", fname, lname);
        });
    }

    [TestMethod]
    public void ConsoleTester_HelloWorld_NoInput()
    {
        const string view = "Hello World";

        ConsoleAssert.Expect(view, () =>
        {
            System.Console.Write("Hello World");
        }, NormalizeOptions.None);
    }

    [TestMethod]
    public async Task ConsoleTester_HelloWorldAsync_NoInput()
    {
        const string view = "Hello World";

        await ConsoleAssert.ExpectAsync(view, async () =>
        {
            await Task.Yield();
            System.Console.Write("Hello World");
        }, NormalizeOptions.None);
    }

    [TestMethod]
    public void ConsoleTester_HelloWorld_TrimLF()
    {
        const string view = "Hello World\n";

        ConsoleAssert.Expect(view, () =>
        {
            System.Console.WriteLine("Hello World");
        });
    }

    [TestMethod]
    [DataRow("\u001b[49mMontoya", "Montoya", true)]
    [DataRow("Inigo\u001b[49mMontoya", "InigoMontoya", true)]
    [DataRow("Inigo\u001b[49m", "Inigo", true)]
    [DataRow("\u001b[49m", "", true)]
    [DataRow("\u001b[101mMontoya", "Montoya", true)]
    [DataRow("\u001b[101mMontoya", "\u001b[101mMontoya", false)]
    public void ConsoleTester_StringWithVT100Characters_VT100Stripped(string input,
        string expected,
        bool stripVT100)
    {
        NormalizeOptions options = NormalizeOptions.NormalizeLineEndings;
        if (stripVT100) options |= NormalizeOptions.StripAnsiEscapeCodes;
        
        ConsoleAssert.Expect(expected, () =>
        {
            System.Console.WriteLine(input);
        }, options);
    }

    [TestMethod]
    public void ConsoleTester_ExplicitStrippingExplicitly_VT100Stripped()
    {
        string input = "\u001b[49mMontoya";
        string expected = "Montoya";

        ConsoleAssert.Expect(expected, () =>
        {
            System.Console.Write(input);
        }, NormalizeOptions.StripAnsiEscapeCodes);
    }

    [TestMethod]
    public void GivenStringLiteral_ExpectedOutputNormalized_OutputMatches()
    {
        const string view = @"Begin
Middle
End";
        ConsoleAssert.Expect(view, () =>
        {
            System.Console.WriteLine("Begin");
            System.Console.WriteLine("Middle");
            System.Console.WriteLine("End");
        });
    }

    [TestMethod]
    public void ConsoleTester_HelloWorld_TrimCRLF()
    {
        const string view = "Hello World";

        ConsoleAssert.Expect(view, () =>
        {
            System.Console.Write("Hello World");
        });
    }

    [TestMethod]
    public void ConsoleTester_HelloWorld_DontNormalizeCRLF()
    {
        const string view = "Hello World\r\n";

        Assert.ThrowsExactly<Exception>(() =>
        {
            ConsoleAssert.Expect(view, () =>
            {
                System.Console.Write("Hello World\r");
            }, NormalizeOptions.None);
        });
    }

    [TestMethod]
    [DataRow("C++")]
    [DataRow("word + word")]
    [DataRow("+hello+world+")]
    public void ConsoleTester_OutputIncludesPluses_PlusesAreNotStripped(string consoleInput)
    {
        Exception exception = Assert.ThrowsExactly<Exception>(() =>
        {
            ConsoleAssert.Expect(consoleInput, () =>
            {
                System.Console.Write(""); // Always fail
            }, NormalizeOptions.None);
        });
        StringAssert.Contains(exception.Message, consoleInput);
    }

    [TestMethod]
    public void ConsoleTester_HelloWorld_MissingNewline()
    {
        const string view = @"Hello World
";

        ConsoleAssert.Expect(view, () =>
        {
            System.Console.WriteLine("Hello World");
        }, NormalizeOptions.None);
    }

    [TestMethod]
    public void ExecuteProcess_PingLocalhost_Success()
    {
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
            expected = $@"PING *(::1) 56 data bytes
64 bytes from * (::1): icmp_seq=1 ttl=64 time=* ms*";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            expected = $@"PING *(*): 56 data bytes
64 bytes from *: icmp_seq=? ttl=64 time=* ms*";
        }

        ConsoleAssert.ExecuteProcess(
            expected,
            "ping", pingArgs, out string _, out _);
    }

    [TestMethod]
    public void ExecuteLike_GivenVariableCRLFWithNLComparedToCRNL_Success()
    {
        const string expected = "(abstract, 1)\n(abstract, 2)\n\n";
        const string output = "(abstract, 1)\r\n(abstract, 2)\r\n";

        ConsoleAssert.ExpectLike(expected, () =>
        {
            System.Console.WriteLine(output);
        });
    }

    [TestMethod]
    public void ExecuteAsync_GivenVariableCRLFWithNLComparedToCRNL_Success()
    {
        const string expected = "(abstract, 1)\n(abstract, 2)\n\n";
        const string output = "(abstract, 1)\r\n(abstract, 2)\r\n";

        ConsoleAssert.ExpectLike(expected, () =>
        {
            System.Console.WriteLine(output);
        });
    }
}