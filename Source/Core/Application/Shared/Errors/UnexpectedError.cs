namespace Application.Shared.Errors;

public record UnexpectedError : ApplicationError
{
    public UnexpectedError(string message, Exception? exception = null) : base(nameof(UnexpectedError), message, exception)
    {
    }
}