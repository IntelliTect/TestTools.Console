using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace IntelliTect.TestTools.Console.Tests;

[TestClass]
public class WildcardMatchAnalyzerTests
{
    [TestMethod]
    public void AnalyzeMatch_SingleLineMatch_IdentifiesWildcardMatches()
    {
        // Arrange
        string expected = "Hello * world";
        string actual = "Hello beautiful world";

        // Act
        var results = WildcardMatchAnalyzer.AnalyzeMatch(expected, actual);

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.IsTrue(results[0].IsMatch);
        Assert.AreEqual(1, results[0].WildcardMatches.Count);
        // The * matches "beautiful" (without trailing space because "world" comes next)
        Assert.AreEqual("beautiful", results[0].WildcardMatches[0].MatchedText);
    }

    [TestMethod]
    public void AnalyzeMatch_MultipleWildcards_TracksAllMatches()
    {
        // Arrange
        string expected = "PING *(* (::1)) * data bytes";
        string actual = "PING localhost(localhost (::1)) 56 data bytes";

        // Act
        var results = WildcardMatchAnalyzer.AnalyzeMatch(expected, actual);

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.IsTrue(results[0].IsMatch);
        Assert.IsTrue(results[0].WildcardMatches.Count >= 2);
    }

    [TestMethod]
    public void AnalyzeMatch_Mismatch_IdentifiesFailure()
    {
        // Arrange
        string expected = "Hello world";
        string actual = "Hello universe";

        // Act
        var results = WildcardMatchAnalyzer.AnalyzeMatch(expected, actual);

        // Assert
        Assert.AreEqual(1, results.Count);
        Assert.IsFalse(results[0].IsMatch);
    }

    [TestMethod]
    public void AnalyzeMatch_ExtraLinesInActual_MarkedAsUnexpected()
    {
        // Arrange
        string expected = "Line 1\nLine 2";
        string actual = "Line 1\nLine 2\nLine 3";

        // Act
        var results = WildcardMatchAnalyzer.AnalyzeMatch(expected, actual);

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.IsTrue(results[0].IsMatch);
        Assert.IsTrue(results[1].IsMatch);
        Assert.IsFalse(results[2].IsMatch); // Extra line
        Assert.IsNull(results[2].ExpectedLine);
        Assert.IsNotNull(results[2].ActualLine);
    }

    [TestMethod]
    public void AnalyzeMatch_MissingLinesInActual_MarkedAsMissing()
    {
        // Arrange
        string expected = "Line 1\nLine 2\nLine 3";
        string actual = "Line 1\nLine 2";

        // Act
        var results = WildcardMatchAnalyzer.AnalyzeMatch(expected, actual);

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.IsTrue(results[0].IsMatch);
        Assert.IsTrue(results[1].IsMatch);
        Assert.IsFalse(results[2].IsMatch); // Missing line
        Assert.IsNotNull(results[2].ExpectedLine);
        Assert.IsNull(results[2].ActualLine);
    }

    [TestMethod]
    public void GenerateDetailedDiff_CreatesReadableOutput()
    {
        // Arrange
        string expected = "Hello * world\nLine *";
        string actual = "Hello beautiful world\nLine 2";

        var results = WildcardMatchAnalyzer.AnalyzeMatch(expected, actual);

        // Act
        string diff = WildcardMatchAnalyzer.GenerateDetailedDiff(results);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(diff));
        StringAssert.Contains(diff, "Line-by-line comparison");
        StringAssert.Contains(diff, "✅"); // Should contain success markers
        StringAssert.Contains(diff, "Wildcard matches");
    }

    [TestMethod]
    public void GenerateDetailedDiff_WithMismatch_ShowsFailure()
    {
        // Arrange
        string expected = "Expected text";
        string actual = "Actual text";

        var results = WildcardMatchAnalyzer.AnalyzeMatch(expected, actual);

        // Act
        string diff = WildcardMatchAnalyzer.GenerateDetailedDiff(results);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(diff));
        StringAssert.Contains(diff, "❌"); // Should contain failure marker
    }
}
