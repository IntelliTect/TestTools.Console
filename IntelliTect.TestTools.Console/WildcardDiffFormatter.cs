using System;
using System.Linq;
using System.Text;

namespace IntelliTect.TestTools.Console;

/// <summary>
/// Formats enhanced diff output for wildcard pattern matching failures.
/// </summary>
internal static class WildcardDiffFormatter
{
    /// <summary>
    /// Formats a detailed diff output showing line-by-line comparison with wildcard matches.
    /// </summary>
    /// <param name="diffResult">The result from WildcardDiffAnalyzer</param>
    /// <param name="equivalentOperatorErrorMessage">The error message prefix</param>
    /// <returns>Formatted diff output string</returns>
    public static string FormatEnhancedDiff(WildcardDiffAnalyzer.DiffResult diffResult, string equivalentOperatorErrorMessage)
    {
        var result = new StringBuilder();
        
        result.AppendLine($"{equivalentOperatorErrorMessage}: Output does not match expected pattern using wildcards.");
        result.AppendLine();
        
        int lineNumber = 1;
        
        foreach (var lineResult in diffResult.LineResults)
        {
            result.AppendLine("────────────────────────────────────────────────────────────");
            result.AppendLine($"Line {lineNumber}:");
            
            if (string.IsNullOrEmpty(lineResult.ExpectedLine) && !string.IsNullOrEmpty(lineResult.ActualLine))
            {
                // Extra line in actual
                result.AppendLine($"Expected: <no more lines>");
                result.AppendLine($"Actual  : {lineResult.ActualLine}");
                result.AppendLine($"Match   : ❌ (unexpected extra line)");
            }
            else if (!string.IsNullOrEmpty(lineResult.ExpectedLine) && string.IsNullOrEmpty(lineResult.ActualLine))
            {
                // Missing line in actual
                result.AppendLine($"Expected: {lineResult.ExpectedLine}");
                result.AppendLine($"Actual  : <missing line>");
                result.AppendLine($"Match   : ❌ (missing line)");
            }
            else
            {
                // Regular line comparison
                result.AppendLine($"Expected: {lineResult.ExpectedLine}");
                result.AppendLine($"Actual  : {lineResult.ActualLine}");
                
                if (lineResult.IsMatch)
                {
                    result.AppendLine("Match   : ✅");
                    
                    // Show what wildcards matched if there are any
                    if (lineResult.WildcardMatches.Count > 0)
                    {
                        result.AppendLine();
                        result.AppendLine("Wildcard matches:");
                        for (int i = 0; i < lineResult.WildcardMatches.Count; i++)
                        {
                            result.AppendLine($"  * => \"{lineResult.WildcardMatches[i]}\"");
                        }
                    }
                }
                else
                {
                    result.AppendLine("Match   : ❌");
                    
                    if (!string.IsNullOrEmpty(lineResult.MismatchReason))
                    {
                        result.AppendLine($"Reason  : {lineResult.MismatchReason}");
                    }
                }
            }
            
            result.AppendLine();
            lineNumber++;
        }
        
        // Summary
        result.AppendLine("────────────────────────────────────────────────────────────");
        result.AppendLine("Summary:");
        
        int matchedLines = diffResult.LineResults.Count(lr => lr.IsMatch);
        int totalLines = diffResult.LineResults.Count;
        int extraLines = diffResult.ExtraActualLines.Count;
        int missingLines = diffResult.MissingExpectedLines.Count;
        
        if (matchedLines > 0)
        {
            result.AppendLine($"✅ {matchedLines} lines matched");
        }
        
        if (extraLines > 0)
        {
            result.AppendLine($"❌ {extraLines} unexpected lines in actual output");
        }
        
        if (missingLines > 0)
        {
            result.AppendLine($"❌ {missingLines} missing lines in expected output");
        }
        
        if (matchedLines == 0 && extraLines == 0 && missingLines == 0)
        {
            result.AppendLine("❌ No lines matched");
        }
        
        return result.ToString();
    }
}