using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IntelliTect.TestTools.Console.Tests;

[TestClass]
public class EnhancedErrorMessageTests
{
    [TestMethod]
    public void ExpectLike_WildcardMismatch_ShowsDetailedDiff()
    {
        // Arrange - pattern with wildcards that will NOT match
        string expected = "PING *(::1) 56 data bytes\n64 bytes from *";
        
        // Act & Assert
        Exception exception = Assert.ThrowsExactly<Exception>(() =>
        {
            ConsoleAssert.ExpectLike(expected, () =>
            {
                System.Console.Write("PING localhost(::1) WRONG data bytes\n64 bytes from localhost");
            });
        });

        // Assert - Check that the error message contains the enhanced output
        string errorMessage = exception.Message;
        
        // Should contain the line-by-line comparison header
        StringAssert.Contains(errorMessage, "Line-by-line comparison");
        
        // Should contain status indicators
        StringAssert.Contains(errorMessage, "❌");  // Lines don't match
    }

    [TestMethod]
    public void ExpectLike_ExtraLines_IdentifiedAsSuch()
    {
        // Arrange - pattern expecting one line, but getting two
        string expected = "Line 1";
        
        // Act & Assert
        Exception exception = Assert.ThrowsExactly<Exception>(() =>
        {
            ConsoleAssert.ExpectLike(expected, () =>
            {
                System.Console.WriteLine("Line 1");
                System.Console.WriteLine("Line 2");
            });
        });

        // Assert
        string errorMessage = exception.Message;
        // With line-by-line comparison, we should see the extra line marked
        StringAssert.Contains(errorMessage, "Line 2: ❌");
        StringAssert.Contains(errorMessage, "Unexpected extra line");
    }

    [TestMethod]
    public void ExpectLike_MissingLines_IdentifiedAsSuch()
    {
        // Arrange - pattern with wildcards expecting more lines
        string expected = "Line 1\nLine *";
        
        // Act & Assert
        Exception exception = Assert.ThrowsExactly<Exception>(() =>
        {
            ConsoleAssert.ExpectLike(expected, () =>
            {
                System.Console.WriteLine("Line 1");
            });
        });

        // Assert
        string errorMessage = exception.Message;
        StringAssert.Contains(errorMessage, "Missing line");
    }

    [TestMethod]
    public void ExecuteProcess_WildcardMismatch_ShowsDetailedDiff()
    {
        // This test demonstrates the improved error output for ExecuteProcess
        // when wildcard matching fails.
        
        string expected = "This * should match";
        
        Exception exception = null;
        try
        {
            ConsoleAssert.ExecuteProcess(
                expected,
                "echo", 
                "Output that does not match",
                out string _, 
                out _);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Assert
        Assert.IsNotNull(exception);
        string errorMessage = exception.Message;
        
        // Should contain wildcard matching error message
        StringAssert.Contains(errorMessage, "wildcard");
        
        // Should contain the line-by-line comparison
        StringAssert.Contains(errorMessage, "Line-by-line comparison");
    }
}
