using Application.Shared.Errors;

namespace Application.UseCases.CheckPulse.Errors;

public record EmptyVitalsError : ApplicationError
{
    public EmptyVitalsError(Exception? exception = null) : base(nameof(EmptyVitalsError), "No vitals found", exception)
    {
    }
}