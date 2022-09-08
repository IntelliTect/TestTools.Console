// This code was originally sourced from https://github.com/PowerShell/PowerShell/blob/main/src/System.Management.Automation/engine/regex.cs
// and then modified to remove of PowerShell specific elements.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


namespace IntelliTect.TestTools.Console;

/// <summary>
/// Convert a string with wild cards into its equivalent regex
/// </summary>
/// <remarks>
/// A list of glob patterns and their equivalent regexes
///
///  glob pattern      regex
/// -------------     -------
/// *foo*              foo
/// foo                ^foo$
/// foo*bar            ^foo.*bar$
/// foo`*bar           ^foo\*bar$
///
/// for a more cases see the unit-test file RegexTest.cs
/// </remarks>
internal class WildcardPatternToRegexParser : WildcardPatternParser
{
    private StringBuilder _regexPattern;
    private RegexOptions _regexOptions;

    private const string regexChars = "()[.?*{}^$+|\\"; // ']' is missing on purpose

    private static bool IsRegexChar(char ch)
    {
        for (int i = 0; i < regexChars.Length; i++)
        {
            if (ch == regexChars[i])
            {
                return true;
            }
        }

        return false;
    }

    internal static RegexOptions TranslateWildcardOptionsIntoRegexOptions(WildcardOptions options)
    {
        RegexOptions regexOptions = RegexOptions.Singleline;

        if ((options & WildcardOptions.Compiled) != 0)
        {
            regexOptions |= RegexOptions.Compiled;
        }
        if ((options & WildcardOptions.IgnoreCase) != 0)
        {
            regexOptions |= RegexOptions.IgnoreCase;
        }
        if ((options & WildcardOptions.CultureInvariant) == WildcardOptions.CultureInvariant)
        {
            regexOptions |= RegexOptions.CultureInvariant;
        }

        return regexOptions;
    }

    protected override void BeginWildcardPattern(WildcardPattern pattern)
    {
        _regexPattern = new StringBuilder((pattern.Pattern.Length * 2) + 2);
        _regexPattern.Append('^');

        _regexOptions = TranslateWildcardOptionsIntoRegexOptions(pattern.Options);
    }

    internal static void AppendLiteralCharacter(StringBuilder regexPattern, char c)
    {
        if (IsRegexChar(c))
        {
            regexPattern.Append('\\');
        }
        regexPattern.Append(c);
    }

    protected override void AppendLiteralCharacter(char c)
    {
        AppendLiteralCharacter(_regexPattern, c);
    }

    protected override void AppendAsterix()
    {
        _regexPattern.Append(".*");
    }

    protected override void AppendQuestionMark()
    {
        _regexPattern.Append('.');
    }

    protected override void EndWildcardPattern()
    {
        _regexPattern.Append('$');

        // lines below are not strictly necessary and are included to preserve
        // wildcard->regex conversion from PS v1 (i.e. not to break unit tests
        // and not to break backcompatibility).
        string regexPatternString = _regexPattern.ToString();
        if (regexPatternString.Equals("^.*$", StringComparison.Ordinal))
        {
            _regexPattern.Remove(0, 4);
        }
        else
        {
            if (regexPatternString.StartsWith("^.*", StringComparison.Ordinal))
            {
                _regexPattern.Remove(0, 3);
            }
            if (regexPatternString.EndsWith(".*$", StringComparison.Ordinal))
            {
                _regexPattern.Remove(_regexPattern.Length - 3, 3);
            }
        }
    }

    protected override void BeginBracketExpression()
    {
        _regexPattern.Append('[');
    }

    internal static void AppendLiteralCharacterToBracketExpression(StringBuilder regexPattern, char c)
    {
        if (c == '[')
        {
            regexPattern.Append('[');
        }
        else if (c == ']')
        {
            regexPattern.Append(@"\]");
        }
        else if (c == '-')
        {
            regexPattern.Append(@"\x2d");
        }
        else
        {
            AppendLiteralCharacter(regexPattern, c);
        }
    }

    protected override void AppendLiteralCharacterToBracketExpression(char c)
    {
        AppendLiteralCharacterToBracketExpression(_regexPattern, c);
    }

    internal static void AppendCharacterRangeToBracketExpression(
                    StringBuilder regexPattern,
                    char startOfCharacterRange,
                    char endOfCharacterRange)
    {
        AppendLiteralCharacterToBracketExpression(regexPattern, startOfCharacterRange);
        regexPattern.Append('-');
        AppendLiteralCharacterToBracketExpression(regexPattern, endOfCharacterRange);
    }

    protected override void AppendCharacterRangeToBracketExpression(
                    char startOfCharacterRange,
                    char endOfCharacterRange)
    {
        AppendCharacterRangeToBracketExpression(_regexPattern, startOfCharacterRange, endOfCharacterRange);
    }

    protected override void EndBracketExpression()
    {
        _regexPattern.Append(']');
    }

    /// <summary>
    /// Parses a <paramref name="wildcardPattern"/> into a <see cref="Regex"/>
    /// </summary>
    /// <param name="wildcardPattern">Wildcard pattern to parse</param>
    /// <returns>Regular expression equivalent to <paramref name="wildcardPattern"/></returns>
    public static Regex Parse(WildcardPattern wildcardPattern)
    {
        WildcardPatternToRegexParser parser = new WildcardPatternToRegexParser();
        WildcardPatternParser.Parse(wildcardPattern, parser);
        try
        {
            return new Regex(parser._regexPattern.ToString(), parser._regexOptions);
        }
        catch (ArgumentException)
        {
            throw WildcardPatternParser.NewWildcardPatternException(wildcardPattern.Pattern);
        }
    }
}

