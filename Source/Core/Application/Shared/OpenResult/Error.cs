namespace OpenResult;

/// <summary>
/// Represents an error condition in a structured, chainable way.
/// </summary>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Code">Optional code or identifier for programmatic handling.</param>
/// <param name="Exception">Optional <see cref="Exception"/> instance if error was caused by an exception.</param>
/// <param name="InnerError">Optional reference to another <see cref="Error"/> that caused this error.</param>
public record Error(
    string Message = "",
    string? Code = null,
    Exception? Exception = null,
    Error? InnerError = null
)
{
    /// <summary>
    /// Gets the deepest, original error in a chain of linked errors.
    /// </summary>
    public Error Root
    {
        get
        {
            var current = this;
            while (current.InnerError != null)
                current = current.InnerError;
            return current;
        }
    }

    /// <summary>
    /// Returns <c>true</c> if this error wraps an <see cref="Exception"/>.
    /// </summary>
    public bool IsExceptional() => Exception is not null;

    /// <summary>
    /// Returns <c>true</c> if this error wraps an <see cref="Exception"/>, and outputs the exception if present.
    /// </summary>
    /// <param name="exception">The wrapped exception if present; otherwise <c>null</c>.</param>
    public bool IsExceptional(out Exception? exception)
    {
        exception = Exception;
        return Exception is not null;
    }
}