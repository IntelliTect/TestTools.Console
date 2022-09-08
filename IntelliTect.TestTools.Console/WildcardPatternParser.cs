// This code was originally sourced from https://github.com/PowerShell/PowerShell/blob/main/src/System.Management.Automation/engine/regex.cs
// and then modified to remove of PowerShell specific elements.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


namespace IntelliTect.TestTools.Console;

/// <summary>
/// A base class for parsers of <see cref="WildcardPattern"/> patterns.
/// </summary>
internal abstract class WildcardPatternParser
{
    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate
    /// the beginning of the wildcard pattern.
    /// Default implementation simply returns.
    /// </summary>
    /// <param name="pattern">
    /// <see cref="WildcardPattern"/> object that includes both
    /// the text of the pattern (<see cref="WildcardPattern.Pattern"/>)
    /// and the pattern options (<see cref="WildcardPattern.Options"/>)
    /// </param>
    protected virtual void BeginWildcardPattern(WildcardPattern pattern)
    {
    }

    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate that the next
    /// part of the pattern should match
    /// a literal character <paramref name="c"/>.
    /// </summary>
    protected abstract void AppendLiteralCharacter(char c);

    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate that the next
    /// part of the pattern should match
    /// any string, including an empty string.
    /// </summary>
    protected abstract void AppendAsterix();

    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate that the next
    /// part of the pattern should match
    /// any single character.
    /// </summary>
    protected abstract void AppendQuestionMark();

    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate the end of the wildcard pattern.
    /// Default implementation simply returns.
    /// </summary>
    protected virtual void EndWildcardPattern()
    {
    }

    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate
    /// the beginning of a bracket expression.
    /// </summary>
    /// <remarks>
    /// Bracket expressions of <see cref="WildcardPattern"/> are
    /// a greatly simplified version of bracket expressions of POSIX wildcards
    /// (http://www.opengroup.org/onlinepubs/9699919799/functions/fnmatch.html).
    /// Only literal characters and character ranges are supported.
    /// Negation (with either '!' or '^' characters),
    /// character classes ([:alpha:])
    /// and other advanced features are not supported.
    /// </remarks>
    protected abstract void BeginBracketExpression();

    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate that the bracket expression
    /// should include a literal character <paramref name="c"/>.
    /// </summary>
    protected abstract void AppendLiteralCharacterToBracketExpression(char c);

    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate that the bracket expression
    /// should include all characters from character range
    /// starting at <paramref name="startOfCharacterRange"/>
    /// and ending at <paramref name="endOfCharacterRange"/>
    /// </summary>
    protected abstract void AppendCharacterRangeToBracketExpression(
                    char startOfCharacterRange,
                    char endOfCharacterRange);

    /// <summary>
    /// Called from <see cref="Parse"/> method to indicate the end of a bracket expression.
    /// </summary>
    protected abstract void EndBracketExpression();

    /// <summary>
    /// PowerShell v1 and v2 treats all characters inside
    /// <paramref name="brackedExpressionContents"/> as literal characters,
    /// except '-' sign which denotes a range.  In particular it means that
    /// '^', '[', ']' are escaped within the bracket expression and don't
    /// have their regex-y meaning.
    /// </summary>
    /// <param name="brackedExpressionContents"></param>
    /// <param name="bracketExpressionOperators"></param>
    /// <param name="pattern"></param>
    /// <remarks>
    /// This method should be kept "internal"
    /// </remarks>
    internal void AppendBracketExpression(string brackedExpressionContents, string bracketExpressionOperators, string pattern)
    {
        this.BeginBracketExpression();

        int i = 0;
        while (i < brackedExpressionContents.Length)
        {
            if (((i + 2) < brackedExpressionContents.Length)
                && (bracketExpressionOperators[i + 1] == '-'))
            {
                char lowerBound = brackedExpressionContents[i];
                char upperBound = brackedExpressionContents[i + 2];
                i += 3;

                if (lowerBound > upperBound)
                {
                    throw NewWildcardPatternException(pattern);
                }

                this.AppendCharacterRangeToBracketExpression(lowerBound, upperBound);
            }
            else
            {
                this.AppendLiteralCharacterToBracketExpression(brackedExpressionContents[i]);
                i++;
            }
        }

        this.EndBracketExpression();
    }

    /// <summary>
    /// Parses <paramref name="pattern"/>, calling appropriate overloads
    /// in <paramref name="parser"/>
    /// </summary>
    /// <param name="pattern">Pattern to parse</param>
    /// <param name="parser">Parser to call back</param>
    public static void Parse(
        WildcardPattern pattern, WildcardPatternParser parser)
    {
        parser.BeginWildcardPattern(pattern);

        bool previousCharacterIsAnEscape = false;
        bool previousCharacterStartedBracketExpression = false;
        bool insideCharacterRange = false;
        StringBuilder characterRangeContents = null;
        StringBuilder characterRangeOperators = null;
        foreach (char c in pattern.Pattern)
        {
            if (insideCharacterRange)
            {
                if (c == ']' && !previousCharacterStartedBracketExpression && !previousCharacterIsAnEscape)
                {
                    // An unescaped closing square bracket closes the character set.  In other
                    // words, there are no nested square bracket expressions
                    // This is different than the POSIX spec
                    // (at http://www.opengroup.org/onlinepubs/9699919799/functions/fnmatch.html),
                    // but we are keeping this behavior for back-compatibility.

                    insideCharacterRange = false;
                    parser.AppendBracketExpression(characterRangeContents.ToString(), characterRangeOperators.ToString(), pattern.Pattern);
                    characterRangeContents = null;
                    characterRangeOperators = null;
                }
                else if (c != pattern.EscapeCharacter || previousCharacterIsAnEscape)
                {
                    characterRangeContents.Append(c);
                    characterRangeOperators.Append((c == '-') && !previousCharacterIsAnEscape ? '-' : ' ');
                }

                previousCharacterStartedBracketExpression = false;
            }
            else
            {
                if (c == '*' && !previousCharacterIsAnEscape)
                {
                    parser.AppendAsterix();
                }
                else if (c == '?' && !previousCharacterIsAnEscape)
                {
                    parser.AppendQuestionMark();
                }
                else if (c == '[' && !previousCharacterIsAnEscape)
                {
                    insideCharacterRange = true;
                    characterRangeContents = new StringBuilder();
                    characterRangeOperators = new StringBuilder();
                    previousCharacterStartedBracketExpression = true;
                }
                else if (c != pattern.EscapeCharacter || previousCharacterIsAnEscape)
                {
                    parser.AppendLiteralCharacter(c);
                }
            }

            previousCharacterIsAnEscape = (c == pattern.EscapeCharacter) && (!previousCharacterIsAnEscape);
        }

        if (insideCharacterRange)
        {
            throw NewWildcardPatternException(pattern.Pattern);
        }

        if (previousCharacterIsAnEscape)
        {
            if (!pattern.Pattern.Equals($"{pattern.EscapeCharacter}", StringComparison.Ordinal)) // Win7 backcompatibility requires treating '`' pattern as '' pattern when this code was used with PowerShell.
            {
                parser.AppendLiteralCharacter(pattern.Pattern[pattern.Pattern.Length - 1]);
            }
        }

        parser.EndWildcardPattern();
    }

    internal static Exception NewWildcardPatternException(string invalidPattern)
    {
        return new Exception(
                $"The wildcard pattern, '{invalidPattern}', is invalid.");
    }
};

