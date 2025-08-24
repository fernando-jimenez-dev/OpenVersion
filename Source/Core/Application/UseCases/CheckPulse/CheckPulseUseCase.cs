using Application.Shared.Errors;
using Application.UseCases.CheckPulse.Abstractions;
using Application.UseCases.CheckPulse.Errors;
using Microsoft.Extensions.Logging;
using OpenResult;

namespace Application.UseCases.CheckPulse;

/// <summary>
/// A simple use case to verify the application's operational state.
/// Use this class as a reference to start implementing your very own use cases.
/// </summary>
/// <remarks>
/// This template serves as a baseline for implementing basic "pulse check"
/// within the system. It logs a confirmation message along with the
/// provided input and returns a successful result.
/// </remarks>
public class CheckPulseUseCase : ICheckPulseUseCase
{
    private readonly ILogger<CheckPulseUseCase> logger;
    private readonly ICheckPulseRepository checkPulseRepository;

    public CheckPulseUseCase(ILogger<CheckPulseUseCase> logger, ICheckPulseRepository checkPulseRepository)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.checkPulseRepository = checkPulseRepository ?? throw new ArgumentNullException(nameof(checkPulseRepository));
    }

    public async Task<Result> Run(string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result.Failure(ValidationError.For(input, "input cannot be empty"));

        try
        {
            return await HandlePulseCheck(input, cancellationToken);
        }
        catch (Exception exception)
        {
            return UnexpectedErrorOutput(input, exception);
        }
    }

    private async Task<Result> HandlePulseCheck(string input, CancellationToken cancellationToken)
    {
        var vitalReadings = await checkPulseRepository.RetrieveVitalReadings(cancellationToken);

        if (vitalReadings.Length > 0)
        {
            logger.LogInformation("System operational. Input: {Input}", input);
            await checkPulseRepository.SaveNewVitalCheck();
            return Result.Success();
        }

        logger.LogDebug("No vital readings found.");
        return Result.Failure(new EmptyVitalsError());
    }

    private Result UnexpectedErrorOutput(string input, Exception exception)
    {
        var errorMessage = $"Unexpected error during Check Pulse use case. Input: '{input}'";
        logger.LogError(exception, errorMessage);
        return Result.Failure(new UnexpectedError(errorMessage, exception));
    }
}