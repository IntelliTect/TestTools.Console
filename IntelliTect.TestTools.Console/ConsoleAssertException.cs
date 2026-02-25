namespace IntelliTect.TestTools.Console;

/// <summary>
/// Exception thrown when a <see cref="ConsoleAssert"/> assertion fails.
/// </summary>
public sealed class ConsoleAssertException : Exception
{
    /// <inheritdoc />
    public ConsoleAssertException(string message) : base(message) { }

    /// <inheritdoc />
    public ConsoleAssertException(string message, Exception innerException)
        : base(message, innerException) { }
}
