// This code was originally sourced from https://github.com/PowerShell/PowerShell/blob/main/src/System.Management.Automation/engine/regex.cs
// and then modified to remove of PowerShell specific elements.
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


namespace IntelliTect.TestTools.Console;

/// <summary>
/// Provides enumerated values to use to set wildcard pattern
/// matching options.
/// </summary>
[Flags]
public enum WildcardOptions
{
    /// <summary>
    /// Indicates that no special processing is required.
    /// </summary>
    None = 0,

    /// <summary>
    /// Specifies that the wildcard pattern is compiled to an assembly.
    /// This yields faster execution but increases startup time.
    /// </summary>
    Compiled = 1,

    /// <summary>
    /// Specifies case-insensitive matching.
    /// </summary>
    IgnoreCase = 2,

    /// <summary>
    /// Specifies culture-invariant matching.
    /// </summary>
    CultureInvariant = 4
};

/// <summary>
/// Represents a wildcard pattern.
/// </summary>
public sealed class WildcardPattern
{
    /// <summary>
    /// char that escapes special chars
    /// </summary>
    public char? EscapeCharacter { get; set; }
    
    /// <summary>
    /// we convert a wildcard pattern to a predicate
    /// </summary>
    private Predicate<string> _isMatch;

    /// <summary>
    /// wildcard pattern
    /// </summary>
    internal string Pattern { get; }
    
    /// <summary>
    /// options that control match behavior
    /// </summary>
    internal WildcardOptions Options { get; } = WildcardOptions.None;

    /// <summary>
    /// wildcard pattern converted to regex pattern.
    /// </summary>
    internal string PatternConvertedToRegex
    {
        get
        {
            var patternRegex = WildcardPatternToRegexParser.Parse(this);
            return patternRegex.ToString();
        }
    }

    /// <summary>
    /// Initializes and instance of the WildcardPattern class
    /// for the specified wildcard pattern.
    /// </summary>
    /// <param name="pattern">The wildcard pattern to match</param>
    /// <returns>The constructed WildcardPattern object</returns>
    /// <remarks> if wildCardType == None, the pattern does not have wild cards</remarks>
    public WildcardPattern(string pattern) :
        this(pattern, WildcardOptions.None)
    { }

    /// <summary>
    /// Initializes an instance of the WildcardPattern class for
    /// the specified wildcard pattern expression, with options
    /// that modify the pattern.
    /// </summary>
    /// <param name="pattern">The wildcard pattern to match.</param>
    /// <param name="escapeCharacter">The escape character for the pattern.</param>
    /// <returns>The constructed WildcardPattern object</returns>
    /// <remarks> if wildCardType == None, the pattern does not have wild cards  </remarks>
    public WildcardPattern(string pattern, char escapeCharacter) :
        this(pattern, escapeCharacter, WildcardOptions.None)
    { }

    /// <summary>
    /// Initializes an instance of the WildcardPattern class for
    /// the specified wildcard pattern expression, with options
    /// that modify the pattern.
    /// </summary>
    /// <param name="pattern">The wildcard pattern to match.</param>
    /// <param name="options">Wildcard options</param>
    /// <returns>The constructed WildcardPattern object</returns>
    /// <remarks> if wildCardType == None, the pattern does not have wild cards  </remarks>
    public WildcardPattern(string pattern, WildcardOptions options)
    {
        Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        Options = options;
    }

    /// <summary>
    /// Initializes an instance of the WildcardPattern class for
    /// the specified wildcard pattern expression, with options
    /// that modify the pattern.
    /// </summary>
    /// <param name="pattern">The wildcard pattern to match.</param>
    /// <param name="escapeCharacter">The escape character for the pattern.</param>
    /// <param name="options">Wildcard options</param>
    /// <returns>The constructed WildcardPattern object</returns>
    /// <remarks> if wildCardType == None, the pattern does not have wild cards  </remarks>
    public WildcardPattern(string pattern, char escapeCharacter,
        WildcardOptions options = WildcardOptions.None) :
        this(pattern, options)
    {
        EscapeCharacter = escapeCharacter;

        bool previousCharacterWasEscape = false;
        foreach (char character in pattern)
        {
            if (character == EscapeCharacter)
            {
                previousCharacterWasEscape = true;
            }
            else
            {
                if (previousCharacterWasEscape)
                {
                    if (!IsWildcardChar(character))
                    {
                        throw new ArgumentException(
                            $"{nameof(pattern)} contains escape characters, '{EscapeCharacter}', with non-wildcard characters.");
                    }
                }
                previousCharacterWasEscape = false;
            }
        }
    }

    private static readonly WildcardPattern _MatchAllIgnoreCasePattern = new("*", WildcardOptions.None);

    /// <summary>
    /// Create a new WildcardPattern, or return an already created one.
    /// </summary>
    /// <param name="pattern">The pattern</param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static WildcardPattern Get(string pattern, WildcardOptions options)
    {
        if (pattern is null)
            throw new ArgumentNullException(nameof(pattern));

        if (pattern.Length == 1 && pattern[0] == '*')
            return _MatchAllIgnoreCasePattern;

        return new WildcardPattern(pattern, options);
    }

    /// <summary>
    /// Instantiate internal regex member if not already done.
    /// </summary>
    ///
    /// <returns> true on success, false otherwise </returns>
    ///
    /// <remarks>  </remarks>
    ///
    private void Init()
    {
        if (_isMatch is null)
        {
            if (Pattern.Length == 1 && Pattern[0] == '*')
            {
                _isMatch = _ => true;
            }
            else
            {
                var matcher = new WildcardPatternMatcher(this);
                _isMatch = matcher.IsMatch;
            }
        }
    }

    /// <summary>
    /// Indicates whether the wildcard pattern specified in the WildcardPattern
    /// constructor finds a match in the input string.
    /// </summary>
    /// <param name="input">The string to search for a match.</param>
    /// <returns>true if the wildcard pattern finds a match; otherwise, false</returns>
    public bool IsMatch(string input)
    {
        Init();
        return input != null && _isMatch(input);
    }

    /// <summary>
    /// Escape special chars, except for those specified in <paramref name="charsNotToEscape"/>, in a string by replacing them with their escape codes.
    /// </summary>
    /// <param name="pattern">The input string containing the text to convert.</param>
    /// <param name="charsNotToEscape">Array of characters that not to escape</param>
    ///  <param name="escapeCharacter">The escape character</param>
    /// <returns>
    /// A string of characters with any metacharacters, except for those specified in <paramref name="charsNotToEscape"/>, converted to their escaped form.
    /// </returns>
    internal static string Escape(string pattern,
        char[] charsNotToEscape, char escapeCharacter)
    {
#pragma warning disable 56506

        if (pattern is null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        if (charsNotToEscape is null)
        {
            throw new ArgumentNullException(nameof(charsNotToEscape));
        }

        char[] temp = new char[(pattern.Length * 2) + 1];
        int tempIndex = 0;

        for (int i = 0; i < pattern.Length; i++)
        {
            char ch = pattern[i];
            
            //
            // if it is a wildcard char, escape it
            //
            if (IsWildcardChar(ch) && !charsNotToEscape.Contains(ch))
            {
                temp[tempIndex++] = escapeCharacter;
            }

            temp[tempIndex++] = ch;
        }

        if (tempIndex > 0)
        {
            return new string(temp, 0, tempIndex);
        }
        else
        {
            return String.Empty;
        }

#pragma warning restore 56506
    }

    /// <summary>
    /// Escape special chars in a string by replacing them with their escape codes.
    /// </summary>
    /// <param name="pattern">The input string containing the text to convert.</param>
    /// <param name="escapeCharacter">Allows for overriding the default escape character</param>
    /// <returns>
    /// A string of characters with any metacharacters converted to their escaped form.
    /// </returns>
    public static string Escape(
        string pattern, char escapeCharacter)
    {
        return Escape(pattern, new char[] { }, escapeCharacter);
    }

    /// <summary>
    /// Checks to see if the given string has any wild card characters in it.
    /// </summary>
    /// <param name="pattern">
    /// String which needs to be checked for the presence of wildcard chars
    /// </param>
    /// <param name="escapeCharacter">Allows for overriding the default escape character</param>
    /// <returns> true if the string has wild card chars, false otherwise. </returns>
    /// <remarks>
    /// Currently { '*', '?', '[', and ']' }, are considered wild card chars.
    /// To override the default escape character, specify the <paramref name="escapeCharacter"/> value.
    /// </remarks>
    public static bool ContainsWildcardCharacters(string pattern,
        char escapeCharacter)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        bool result = false;

        for (int index = 0; index < pattern.Length; ++index)
        {
            if (IsWildcardChar(pattern[index]))
            {
                result = true;
                break;
            }

            // If it is an escape character then advance past
            // the next character

            if (pattern[index] == escapeCharacter)
            {
                ++index;
            }
        }
        return result;
    }

    /// <summary>
    /// Unescapes any escaped characters in the input string.
    /// </summary>
    /// <param name="pattern">
    /// The input string containing the text to convert.
    /// </param>
    /// <param name="escapeCharacter">Allows for overriding the default escape character</param>
    /// <returns>
    /// A string of characters with any escaped characters
    /// converted to their unescaped form.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// If <paramref name="pattern" /> is null.
    /// </exception>
    public static string Unescape(
        string pattern, char escapeCharacter)
    {
        if (pattern is null)
        {
            throw new ArgumentNullException(nameof(pattern));
        }

        char[] temp = new char[pattern.Length];
        int tempIndex = 0;
        bool prevCharWasEscapeChar = false;

        for (int i = 0; i < pattern.Length; i++)
        {
            char ch = pattern[i];
            
            if (ch == escapeCharacter)
            {
                if (prevCharWasEscapeChar)
                {
                    temp[tempIndex++] = ch;
                    prevCharWasEscapeChar = false;
                }
                else
                {
                    prevCharWasEscapeChar = true;
                }
                continue;
            }

            if (prevCharWasEscapeChar)
            {
                if (!IsWildcardChar(ch))
                {
                    temp[tempIndex++] = escapeCharacter;
                }
            }

            temp[tempIndex++] = ch;
            prevCharWasEscapeChar = false;
        }

        // Need to account for a trailing escape character as a real
        // character

        if (prevCharWasEscapeChar)
        {
            temp[tempIndex++] = escapeCharacter;
            prevCharWasEscapeChar = false;
        }

        if (tempIndex > 0)
        {
            return new string(temp, 0, tempIndex);
        }
        else
        {
            return string.Empty;
        }
    }

    public static bool IsWildcardChar(char ch)
    {
        return WildCardCharacters.Contains(ch);
    }

    public const string WildCardCharacters = "*?[]";
}

