using Microsoft.Extensions.Logging;
using OpenResult;

namespace Application.Shared.Errors;

public record ApplicationError : Error
{
    public Guid Id { get; set; }
    public string Type { get; set; }

    /// <summary>
    /// All application errors are expected to be logged at some point during the request lifecycle.
    /// To support meaningful observability and monitoring automation, each error declares its severity
    /// via the <see cref="Severity" /> property.
    /// <br/><br/>
    ///
    /// This severity determines how our monitoring system reacts to the error:
    /// <br/> - <see cref="LogLevel.Error" />: Triggers automatic triage and ticket creation.
    /// <br/> - <see cref="LogLevel.Warning" />: Logged for context but does not trigger action.
    /// <br/> - <see cref="LogLevel.Critical" />: Indicates unrecoverable failure or system instability.
    ///
    /// <br/><br/>
    /// Override the <see cref="Severity" /> property in your error subtype if it represents a
    /// non-critical issue (e.g., invalid input, resource not found, etc.).
    /// </summary>
    public virtual LogLevel Severity => LogLevel.Error;

    public ApplicationError(string type, string message, Exception? exception = null) : base(message)
    {
        Id = Guid.NewGuid();
        Type = type;
        Exception = exception;
    }
}