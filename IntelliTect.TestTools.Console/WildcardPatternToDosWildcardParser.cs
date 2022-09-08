// This code was originally sourced from https://github.com/PowerShell/PowerShell/blob/main/src/System.Management.Automation/engine/regex.cs
// and then modified to remove of PowerShell specific elements.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


namespace IntelliTect.TestTools.Console;

/// <summary>
/// Translates a <see cref="WildcardPattern"/> into a DOS wildcard
/// </summary>
internal class WildcardPatternToDosWildcardParser : WildcardPatternParser
{
    private readonly StringBuilder _result = new StringBuilder();

    protected override void AppendLiteralCharacter(char c)
    {
        _result.Append(c);
    }

    protected override void AppendAsterix()
    {
        _result.Append('*');
    }

    protected override void AppendQuestionMark()
    {
        _result.Append('?');
    }

    protected override void BeginBracketExpression()
    {
    }

    protected override void AppendLiteralCharacterToBracketExpression(char c)
    {
    }

    protected override void AppendCharacterRangeToBracketExpression(char startOfCharacterRange, char endOfCharacterRange)
    {
    }

    protected override void EndBracketExpression()
    {
        _result.Append('?');
    }

    /// <summary>
    /// Converts <paramref name="wildcardPattern"/> into a DOS wildcard
    /// </summary>
    internal static string Parse(WildcardPattern wildcardPattern)
    {
        var parser = new WildcardPatternToDosWildcardParser();
        WildcardPatternParser.Parse(wildcardPattern, parser);
        return parser._result.ToString();
    }
}

