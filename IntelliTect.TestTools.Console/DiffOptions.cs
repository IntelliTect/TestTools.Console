namespace IntelliTect.TestTools.Console;

/// <summary>
/// Provides options for controlling diff output behavior when wildcard matching fails.
/// </summary>
[Flags]
public enum DiffOptions
{
    /// <summary>
    /// Use the default diff output format.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Enable enhanced line-by-line diff output with wildcard match tracking for wildcard patterns.
    /// This provides detailed information about what each wildcard matched and where mismatches occurred.
    /// </summary>
    EnhancedWildcardDiff = 1,
}