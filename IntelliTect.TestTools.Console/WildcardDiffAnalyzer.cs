using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace IntelliTect.TestTools.Console;

/// <summary>
/// Provides detailed analysis of wildcard pattern matching for enhanced diff output.
/// </summary>
internal class WildcardDiffAnalyzer
{
    /// <summary>
    /// Represents the result of matching a single line with a wildcard pattern.
    /// </summary>
    public class LineMatchResult
    {
        public bool IsMatch { get; set; }
        public string ExpectedLine { get; set; } = string.Empty;
        public string ActualLine { get; set; } = string.Empty;
        public List<string> WildcardMatches { get; set; } = new List<string>();
        public string MismatchReason { get; set; }
    }

    /// <summary>
    /// Represents the overall result of comparing expected and actual output with wildcards.
    /// </summary>
    public class DiffResult
    {
        public List<LineMatchResult> LineResults { get; set; } = new List<LineMatchResult>();
        public List<string> ExtraActualLines { get; set; } = new List<string>();
        public List<string> MissingExpectedLines { get; set; } = new List<string>();
        public bool OverallMatch { get; set; }
    }

    /// <summary>
    /// Analyzes the difference between expected pattern and actual output using wildcard matching.
    /// </summary>
    /// <param name="expectedPattern">The expected pattern with wildcards</param>
    /// <param name="actualOutput">The actual output to match against</param>
    /// <param name="escapeCharacter">The escape character for wildcards (default is backslash)</param>
    /// <returns>Detailed analysis of the matching process</returns>
    public static DiffResult AnalyzeDiff(string expectedPattern, string actualOutput, char escapeCharacter = '\\')
    {
        var result = new DiffResult();
        
        var expectedLines = SplitIntoLines(expectedPattern);
        var actualLines = SplitIntoLines(actualOutput);
        
        int expectedIndex = 0;
        int actualIndex = 0;
        
        while (expectedIndex < expectedLines.Count || actualIndex < actualLines.Count)
        {
            if (expectedIndex >= expectedLines.Count)
            {
                // Extra actual lines
                result.ExtraActualLines.Add(actualLines[actualIndex]);
                result.LineResults.Add(new LineMatchResult
                {
                    IsMatch = false,
                    ExpectedLine = "",
                    ActualLine = actualLines[actualIndex],
                    MismatchReason = "unexpected extra line"
                });
                actualIndex++;
            }
            else if (actualIndex >= actualLines.Count)
            {
                // Missing expected lines
                result.MissingExpectedLines.Add(expectedLines[expectedIndex]);
                result.LineResults.Add(new LineMatchResult
                {
                    IsMatch = false,
                    ExpectedLine = expectedLines[expectedIndex],
                    ActualLine = "",
                    MismatchReason = "missing line"
                });
                expectedIndex++;
            }
            else
            {
                // Try to match current lines
                var lineResult = MatchLineWithWildcards(expectedLines[expectedIndex], actualLines[actualIndex], escapeCharacter);
                result.LineResults.Add(lineResult);
                
                expectedIndex++;
                actualIndex++;
            }
        }
        
        result.OverallMatch = result.LineResults.All(lr => lr.IsMatch);
        return result;
    }

    /// <summary>
    /// Matches a single line against a wildcard pattern and tracks what each wildcard matched.
    /// </summary>
    private static LineMatchResult MatchLineWithWildcards(string expectedPattern, string actualLine, char escapeCharacter)
    {
        var result = new LineMatchResult
        {
            ExpectedLine = expectedPattern,
            ActualLine = actualLine
        };

        // Use the existing IsLike method to check if it matches
        bool isMatch = actualLine.IsLike(expectedPattern, escapeCharacter);
        result.IsMatch = isMatch;

        if (isMatch)
        {
            // Try to extract what each wildcard matched, but if it fails, just indicate matches exist
            try
            {
                result.WildcardMatches = ExtractWildcardMatches(expectedPattern, actualLine, escapeCharacter);
            }
            catch (Exception)
            {
                // If extraction fails, just count the wildcards and provide placeholder text
                int wildcardCount = CountWildcards(expectedPattern, escapeCharacter);
                result.WildcardMatches = new List<string>();
                for (int i = 0; i < wildcardCount; i++)
                {
                    result.WildcardMatches.Add("<matched content>");
                }
            }
        }
        else
        {
            result.MismatchReason = "pattern does not match";
        }

        return result;
    }

    /// <summary>
    /// Counts the number of wildcards in a pattern.
    /// </summary>
    private static int CountWildcards(string pattern, char escapeCharacter)
    {
        int count = 0;
        for (int i = 0; i < pattern.Length; i++)
        {
            if (pattern[i] == '*' && (i == 0 || pattern[i - 1] != escapeCharacter))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Extracts what each wildcard (*) in the pattern matched in the actual string.
    /// </summary>
    private static List<string> ExtractWildcardMatches(string pattern, string actual, char escapeCharacter)
    {
        var matches = new List<string>();
        
        // Convert wildcard pattern to regex to capture groups
        try
        {
            var regexPattern = ConvertWildcardToRegex(pattern, escapeCharacter);
            var regex = new Regex(regexPattern, RegexOptions.Singleline);
            var match = regex.Match(actual);
            
            if (match.Success)
            {
                // Skip group 0 (the entire match) and collect captured groups
                for (int i = 1; i < match.Groups.Count; i++)
                {
                    matches.Add(match.Groups[i].Value);
                }
            }
            else
            {
                // Fallback: count wildcards and provide placeholders
                int wildcardCount = CountWildcards(pattern, escapeCharacter);
                for (int i = 0; i < wildcardCount; i++)
                {
                    matches.Add("<matched content>");
                }
            }
        }
        catch (Exception)
        {
            // If regex conversion fails, fall back to basic tracking
            int wildcardCount = CountWildcards(pattern, escapeCharacter);
            for (int i = 0; i < wildcardCount; i++)
            {
                matches.Add("<matched content>");
            }
        }
        
        return matches;
    }

    /// <summary>
    /// Converts a wildcard pattern to a regex pattern with capture groups for each wildcard.
    /// </summary>
    private static string ConvertWildcardToRegex(string wildcardPattern, char escapeCharacter)
    {
        var result = new StringBuilder();
        result.Append("^");
        
        for (int i = 0; i < wildcardPattern.Length; i++)
        {
            char c = wildcardPattern[i];
            
            if (c == '*' && (i == 0 || wildcardPattern[i - 1] != escapeCharacter))
            {
                // Unescaped wildcard - convert to capturing group
                result.Append("(.*?)");
            }
            else if (c == '?' && (i == 0 || wildcardPattern[i - 1] != escapeCharacter))
            {
                // Unescaped single character wildcard
                result.Append(".");
            }
            else if (c == escapeCharacter && i + 1 < wildcardPattern.Length)
            {
                // Escaped character - add the next character literally
                i++; // Skip the escape character
                result.Append(Regex.Escape(wildcardPattern[i].ToString()));
            }
            else
            {
                // Regular character - escape for regex
                result.Append(Regex.Escape(c.ToString()));
            }
        }
        
        result.Append("$");
        return result.ToString();
    }

    /// <summary>
    /// Splits text into lines, preserving empty lines.
    /// </summary>
    private static List<string> SplitIntoLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string>();
            
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None).ToList();
        
        // Remove trailing empty line if it's the last one and was created by a trailing newline
        if (lines.Count > 0 && string.IsNullOrEmpty(lines[lines.Count - 1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }
        
        return lines;
    }
}