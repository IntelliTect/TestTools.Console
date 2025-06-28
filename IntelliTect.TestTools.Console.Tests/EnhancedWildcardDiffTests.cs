using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Runtime.InteropServices;

namespace IntelliTect.TestTools.Console.Tests;

[TestClass]
public class EnhancedWildcardDiffTests
{
    [TestMethod]
    public void ExpectLike_WithEnhancedDiff_ShowsDetailedOutput()
    {
        // Arrange: Create a test that will fail to see the enhanced output
        string expected = @"PING *(* (::1)) 56 data bytes
64 bytes from * (::1): icmp_seq=1 ttl=64 time=* ms
Response time was * ms";

        string actualOutput = @"PING localhost (::1) 56 data bytes
64 bytes from localhost (::1): icmp_seq=1 ttl=64 time=0.018 ms
Extra line that shouldn't be here
Response time was 0.025 ms";

        // Act & Assert
        Exception exception = Assert.ThrowsException<Exception>(() =>
        {
            ConsoleAssert.ExpectLike(expected, () =>
            {
                System.Console.Write(actualOutput);
            }, DiffOptions.EnhancedWildcardDiff);
        });

        // Verify the enhanced output contains line-by-line information
        string message = exception.Message;
        StringAssert.Contains(message, "Line 1:");
        StringAssert.Contains(message, "Line 2:");
        StringAssert.Contains(message, "Line 3:");
        StringAssert.Contains(message, "✅"); // Should contain success markers
        StringAssert.Contains(message, "❌"); // Should contain failure markers
        StringAssert.Contains(message, "unexpected extra line"); // Should identify extra lines
        StringAssert.Contains(message, "Summary:");
    }

    [TestMethod]
    public void ExpectLike_WithoutEnhancedDiff_ShowsOriginalOutput()
    {
        // Arrange: Create a test that will fail to see the original output
        string expected = @"PING *(* (::1)) 56 data bytes
64 bytes from * (::1): icmp_seq=1 ttl=64 time=* ms";

        string actualOutput = @"PING localhost (::1) 56 data bytes
64 bytes from localhost (::1): icmp_seq=1 ttl=64 time=0.018 ms
Extra line";

        // Act & Assert
        Exception exception = Assert.ThrowsException<Exception>(() =>
        {
            ConsoleAssert.ExpectLike(expected, () =>
            {
                System.Console.Write(actualOutput);
            }); // Default behavior, no enhanced diff
        });

        // Verify the original output format
        string message = exception.Message;
        StringAssert.Contains(message, "Expected:");
        StringAssert.Contains(message, "Actual  :");
        StringAssert.Contains(message, "-----------------------------------");
        
        // Should NOT contain enhanced diff elements
        Assert.IsFalse(message.Contains("Line 1:"));
        Assert.IsFalse(message.Contains("✅"));
        Assert.IsFalse(message.Contains("❌"));
    }

    [TestMethod]
    public void WildcardDiffAnalyzer_AnalyzeDiff_SimpleMatch()
    {
        // Arrange
        string pattern = "Hello * world";
        string actual = "Hello beautiful world";

        // Act
        var result = WildcardDiffAnalyzer.AnalyzeDiff(pattern, actual);

        // Assert
        Assert.IsTrue(result.OverallMatch);
        Assert.AreEqual(1, result.LineResults.Count);
        Assert.IsTrue(result.LineResults[0].IsMatch);
        Assert.AreEqual(1, result.LineResults[0].WildcardMatches.Count);
        Assert.AreEqual("beautiful", result.LineResults[0].WildcardMatches[0]);
    }

    [TestMethod]
    public void WildcardDiffAnalyzer_AnalyzeDiff_ExtraLines()
    {
        // Arrange
        string pattern = "Line 1\nLine 2";
        string actual = "Line 1\nLine 2\nExtra Line 3";

        // Act
        var result = WildcardDiffAnalyzer.AnalyzeDiff(pattern, actual);

        // Assert
        Assert.IsFalse(result.OverallMatch);
        Assert.AreEqual(3, result.LineResults.Count);
        Assert.IsTrue(result.LineResults[0].IsMatch);
        Assert.IsTrue(result.LineResults[1].IsMatch);
        Assert.IsFalse(result.LineResults[2].IsMatch);
        Assert.AreEqual("unexpected extra line", result.LineResults[2].MismatchReason);
    }

    [TestMethod]
    public void WildcardDiffAnalyzer_AnalyzeDiff_MissingLines()
    {
        // Arrange
        string pattern = "Line 1\nLine 2\nLine 3";
        string actual = "Line 1\nLine 2";

        // Act
        var result = WildcardDiffAnalyzer.AnalyzeDiff(pattern, actual);

        // Assert
        Assert.IsFalse(result.OverallMatch);
        Assert.AreEqual(3, result.LineResults.Count);
        Assert.IsTrue(result.LineResults[0].IsMatch);
        Assert.IsTrue(result.LineResults[1].IsMatch);
        Assert.IsFalse(result.LineResults[2].IsMatch);
        Assert.AreEqual("missing line", result.LineResults[2].MismatchReason);
    }
}