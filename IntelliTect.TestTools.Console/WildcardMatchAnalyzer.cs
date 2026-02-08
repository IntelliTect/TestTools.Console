namespace IntelliTect.TestTools.Console;

/// <summary>
/// Analyzes wildcard pattern matching to provide detailed diff information.
/// </summary>
internal static class WildcardMatchAnalyzer
{
    /// <summary>
    /// Represents the result of matching a single line.
    /// </summary>
    internal class LineMatchResult
    {
        public bool IsMatch { get; set; }
        
        /// <summary>
        /// The expected line pattern. Null if this line exists in actual output but not in expected.
        /// </summary>
        public string ExpectedLine { get; set; }
        
        /// <summary>
        /// The actual line text. Null if this line exists in expected output but not in actual.
        /// </summary>
        public string ActualLine { get; set; }
        
        public List<WildcardMatch> WildcardMatches { get; set; } = new List<WildcardMatch>();
        public string Status => IsMatch ? "✅" : "❌";
    }

    /// <summary>
    /// Represents what a single wildcard matched.
    /// </summary>
    internal class WildcardMatch
    {
        /// <summary>
        /// The wildcard pattern (e.g., "*", "?", or "[...]").
        /// </summary>
        public string Pattern { get; set; } = string.Empty;
        
        /// <summary>
        /// The text that was matched by this wildcard.
        /// </summary>
        public string MatchedText { get; set; } = string.Empty;
        
        public int Position { get; set; }
    }

    /// <summary>
    /// Analyzes wildcard pattern matching line by line and returns detailed results.
    /// </summary>
    public static List<LineMatchResult> AnalyzeMatch(string expectedPattern, string actualText)
    {
        var results = new List<LineMatchResult>();
        
        // Split into lines
        string[] expectedLines = SplitIntoLines(expectedPattern);
        string[] actualLines = SplitIntoLines(actualText);

        int maxLines = Math.Max(expectedLines.Length, actualLines.Length);

        for (int i = 0; i < maxLines; i++)
        {
            string expectedLine = i < expectedLines.Length ? expectedLines[i] : null;
            string actualLine = i < actualLines.Length ? actualLines[i] : null;

            var lineResult = new LineMatchResult
            {
                ExpectedLine = expectedLine,
                ActualLine = actualLine
            };

            if (expectedLine == null)
            {
                // Extra line in actual output
                lineResult.IsMatch = false;
            }
            else if (actualLine == null)
            {
                // Missing line in actual output
                lineResult.IsMatch = false;
            }
            else
            {
                // Try to match the line and capture what wildcards matched
                lineResult.IsMatch = MatchLineWithWildcards(expectedLine, actualLine, lineResult.WildcardMatches);
            }

            results.Add(lineResult);
        }

        return results;
    }

    /// <summary>
    /// Generates a detailed diff message from the match results.
    /// </summary>
    public static string GenerateDetailedDiff(List<LineMatchResult> matchResults)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("Line-by-line comparison:");
        sb.AppendLine("========================");

        for (int i = 0; i < matchResults.Count; i++)
        {
            var result = matchResults[i];
            sb.AppendLine();
            sb.AppendLine($"Line {i + 1}: {result.Status}");

            if (result.ExpectedLine == null)
            {
                sb.AppendLine($"  ⚠️  Unexpected extra line in actual output:");
                sb.AppendLine($"       Actual: {EscapeForDisplay(result.ActualLine)}");
            }
            else if (result.ActualLine == null)
            {
                sb.AppendLine($"  ⚠️  Missing line in actual output:");
                sb.AppendLine($"       Expected: {EscapeForDisplay(result.ExpectedLine)}");
            }
            else
            {
                sb.AppendLine($"  Expected: {EscapeForDisplay(result.ExpectedLine)}");
                sb.AppendLine($"  Actual:   {EscapeForDisplay(result.ActualLine)}");

                if (result.IsMatch && result.WildcardMatches.Count > 0)
                {
                    sb.AppendLine($"  Wildcard matches:");
                    foreach (var match in result.WildcardMatches)
                    {
                        sb.AppendLine($"    '{match.Pattern}' matched: {EscapeForDisplay(match.MatchedText)}");
                    }
                }
                else if (!result.IsMatch)
                {
                    // Try to show where the mismatch occurred
                    string mismatchInfo = FindMismatchPosition(result.ExpectedLine, result.ActualLine);
                    if (!string.IsNullOrEmpty(mismatchInfo))
                    {
                        sb.AppendLine($"  {mismatchInfo}");
                    }
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Tries to match a line with wildcards and captures what each wildcard matched.
    /// This is a simplified implementation that tracks * and ? wildcards.
    /// </summary>
    private static bool MatchLineWithWildcards(string pattern, string text, List<WildcardMatch> wildcardMatches)
    {
        try
        {
            var wildcardPattern = new WildcardPattern(pattern);
            bool isMatch = wildcardPattern.IsMatch(text);

            if (isMatch)
            {
                // Extract what each wildcard matched
                ExtractWildcardMatches(pattern, text, wildcardMatches);
            }

            return isMatch;
        }
        catch (ArgumentException)
        {
            // Invalid pattern - treat as no match
            return false;
        }
    }

    /// <summary>
    /// Extracts what each wildcard in the pattern matched in the text.
    /// This uses a greedy approach to match wildcards.
    /// </summary>
    private static void ExtractWildcardMatches(string pattern, string text, List<WildcardMatch> wildcardMatches)
    {
        int patternPos = 0;
        int textPos = 0;
        int wildcardIndex = 0;

        while (patternPos < pattern.Length && textPos <= text.Length)
        {
            char patternChar = pattern[patternPos];

            if (patternChar == '*')
            {
                // Find the next non-wildcard character in the pattern
                int nextPatternPos = patternPos + 1;
                while (nextPatternPos < pattern.Length && (pattern[nextPatternPos] == '*' || pattern[nextPatternPos] == '?'))
                {
                    nextPatternPos++;
                }

                if (nextPatternPos >= pattern.Length)
                {
                    // * at the end matches everything remaining
                    wildcardMatches.Add(new WildcardMatch
                    {
                        Pattern = "*",
                        MatchedText = text.Substring(textPos),
                        Position = wildcardIndex++
                    });
                    return;
                }

                // Find where the next literal character appears in the text
                string remainingPattern = pattern.Substring(nextPatternPos);
                int nextLiteralIndex = FindNextLiteralMatch(text, textPos, remainingPattern);

                if (nextLiteralIndex == -1)
                {
                    // Could not find a match for the remaining pattern
                    wildcardMatches.Add(new WildcardMatch
                    {
                        Pattern = "*",
                        MatchedText = text.Substring(textPos),
                        Position = wildcardIndex++
                    });
                    return;
                }

                // The * matched everything from textPos to nextLiteralIndex
                wildcardMatches.Add(new WildcardMatch
                {
                    Pattern = "*",
                    MatchedText = text.Substring(textPos, nextLiteralIndex - textPos),
                    Position = wildcardIndex++
                });

                textPos = nextLiteralIndex;
                patternPos++;
            }
            else if (patternChar == '?')
            {
                // ? matches exactly one character
                if (textPos < text.Length)
                {
                    wildcardMatches.Add(new WildcardMatch
                    {
                        Pattern = "?",
                        MatchedText = text[textPos].ToString(),
                        Position = wildcardIndex++
                    });
                    textPos++;
                }
                patternPos++;
            }
            else if (patternChar == '[')
            {
                // Character class - skip to the closing ]
                int closingBracket = pattern.IndexOf(']', patternPos);
                if (closingBracket > patternPos && textPos < text.Length)
                {
                    string charClass = pattern.Substring(patternPos, closingBracket - patternPos + 1);
                    wildcardMatches.Add(new WildcardMatch
                    {
                        Pattern = charClass,
                        MatchedText = text[textPos].ToString(),
                        Position = wildcardIndex++
                    });
                    textPos++;
                    patternPos = closingBracket + 1;
                }
                else
                {
                    patternPos++;
                }
            }
            else
            {
                // Literal character - must match exactly
                if (textPos < text.Length && text[textPos] == patternChar)
                {
                    textPos++;
                }
                patternPos++;
            }
        }
    }

    /// <summary>
    /// Finds the next position where the pattern starts matching in the text.
    /// </summary>
    private static int FindNextLiteralMatch(string text, int startPos, string pattern)
    {
        // Simple heuristic: find the first non-wildcard character in the pattern
        // and search for it in the text
        for (int i = 0; i < pattern.Length; i++)
        {
            char c = pattern[i];
            if (c != '*' && c != '?' && c != '[')
            {
                // Found a literal character, search for it
                int index = text.IndexOf(c, startPos);
                if (index >= 0)
                {
                    return index;
                }
                return -1;
            }
        }
        return -1; // Pattern has no literals
    }

    /// <summary>
    /// Finds the position where pattern and text first differ (for non-wildcard mismatches).
    /// </summary>
    private static string FindMismatchPosition(string expected, string actual)
    {
        int minLength = Math.Min(expected.Length, actual.Length);
        
        for (int i = 0; i < minLength; i++)
        {
            if (expected[i] != actual[i] && expected[i] != '*' && expected[i] != '?')
            {
                return $"Mismatch at position {i}: expected '{EscapeChar(expected[i])}' but got '{EscapeChar(actual[i])}'";
            }
        }

        if (expected.Length != actual.Length)
        {
            return $"Length mismatch: expected {expected.Length} characters but got {actual.Length}";
        }

        return string.Empty;
    }

    /// <summary>
    /// Splits text into lines, preserving line ending information.
    /// </summary>
    private static string[] SplitIntoLines(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<string>();
        }

        return text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    }

    /// <summary>
    /// Escapes a string for display, showing special characters.
    /// </summary>
    private static string EscapeForDisplay(string text)
    {
        if (text == null) return "<null>";
        if (text == string.Empty) return "<empty>";

        return text
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
    }

    /// <summary>
    /// Escapes a single character for display.
    /// </summary>
    private static string EscapeChar(char c)
    {
        return c switch
        {
            '\r' => "\\r",
            '\n' => "\\n",
            '\t' => "\\t",
            _ => c.ToString()
        };
    }
}
