using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IntelliTect.TestTools.Console.Tests;

[TestClass]
public class EnhancedErrorMessageTests
{
    /// <summary>
    /// Runs <paramref name="consoleAction"/> via <see cref="ConsoleAssert.ExpectLike"/>, asserts it
    /// throws <see cref="ConsoleAssertException"/>, and returns the exception message.
    /// </summary>
    private static string GetMismatchMessage(string expected, Action consoleAction)
    {
        var ex = Assert.ThrowsExactly<ConsoleAssertException>(
            () => ConsoleAssert.ExpectLike(expected, consoleAction));
        return ex.Message;
    }

    [TestMethod]
    public void ExpectLike_WildcardMismatch_ShowsDetailedDiff()
    {
        string errorMessage = GetMismatchMessage(
            "PING *(::1) 56 data bytes\n64 bytes from *",
            () => System.Console.Write("PING localhost(::1) WRONG data bytes\n64 bytes from localhost"));

        StringAssert.Contains(errorMessage, "Line-by-line comparison");
        StringAssert.Contains(errorMessage, "❌");
    }

    [TestMethod]
    public void ExpectLike_ExtraLines_IdentifiedAsSuch()
    {
        string errorMessage = GetMismatchMessage(
            "Line 1",
            () =>
            {
                System.Console.WriteLine("Line 1");
                System.Console.WriteLine("Line 2");
            });

        StringAssert.Contains(errorMessage, "Line 2: ❌");
        StringAssert.Contains(errorMessage, "Unexpected extra line");
    }

    [TestMethod]
    public void ExpectLike_MissingLines_IdentifiedAsSuch()
    {
        string errorMessage = GetMismatchMessage(
            "Line 1\nLine *",
            () => System.Console.WriteLine("Line 1"));

        StringAssert.Contains(errorMessage, "Missing line");
    }

    [TestMethod]
    public void ExpectLike_SuccessfulWildcardMatch_DoesNotThrow()
    {
        ConsoleAssert.ExpectLike("Hello * world", () =>
        {
            System.Console.Write("Hello beautiful world");
        });
    }
}
