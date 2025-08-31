using Application.Shared.Errors;
using Application.UseCases.ComputeNextVersion;
using Application.UseCases.ComputeNextVersion.Errors;
using Microsoft.AspNetCore.Mvc;
using OpenResult;
using System.Net;

namespace WebAPI.Minimal.UseCases.ComputeNextVersion;

public class ComputeNextVersionEndpoint
{
    public static async Task<IResult> Execute(
        [FromServices] IComputeNextVersionUseCase computeNextVersionUseCase,
        [FromServices] ILogger<ComputeNextVersionEndpoint> logger,
        [FromBody] IComputeNextVersionUseCase.Input request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await computeNextVersionUseCase.Run(request, cancellationToken);

            return result.IsSuccess
                ? HandleSuccess(result.Value!, logger)
                : HandleError(result.Error!, logger);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "There was an unrecoverable error while computing next version.");
            return CreateErrorResponse("Unrecoverable error encountered.", HttpStatusCode.InternalServerError);
        }
    }

    private static IResult HandleSuccess(IComputeNextVersionUseCase.Output output, ILogger<ComputeNextVersionEndpoint> logger)
    {
        logger.LogInformation("Next version computed: {NextVersion}", output.NextVersion);
        return CreateJsonResponse(new ComputeNextVersionResponse(output.NextVersion), HttpStatusCode.OK);
    }

    private static IResult HandleError(Error error, ILogger<ComputeNextVersionEndpoint> logger)
    {
        switch (error)
        {
            case ValidationError<IComputeNextVersionUseCase.Input> validationError:
                logger.LogWarning("Invalid input for compute next version: {Message}", validationError.Message);
                return CreateJsonResponse(new ComputeNextVersionResponse(validationError.Message), HttpStatusCode.BadRequest);

            case UnsupportedBranchError unsupportedBranchError:
                logger.LogWarning("Unsupported branch: {Branch}", unsupportedBranchError.BranchName);
                return CreateJsonResponse(new ComputeNextVersionResponse(unsupportedBranchError.Message), HttpStatusCode.BadRequest);

            case VersionConcurrencyError concurrencyError:
                logger.LogWarning(concurrencyError.Exception, "Version concurrency conflict while saving version.");
                return CreateJsonResponse(new ComputeNextVersionResponse(concurrencyError.Message), HttpStatusCode.Conflict);

            case UnexpectedError unexpectedError:
                logger.LogError(unexpectedError.Exception, "Unexpected error computing next version: {Message}", unexpectedError.Message);
                return Results.StatusCode((int)HttpStatusCode.InternalServerError);

            case ApplicationError appError:
                logger.Log(appError.Severity, appError.Exception, "Application error computing next version: {Type} - {Message}", appError.Type, appError.Message);
                return Results.StatusCode((int)HttpStatusCode.InternalServerError);

            default:
                logger.LogError("An unknown error occurred while running the Compute Next Version use case. Message: '{Message}'", error?.Message ?? string.Empty);
                return Results.StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }

    private static IResult CreateJsonResponse(ComputeNextVersionResponse response, HttpStatusCode statusCode)
    {
        return Results.Json(data: response, statusCode: (int)statusCode);
    }

    private static IResult CreateErrorResponse(string message, HttpStatusCode statusCode)
    {
        return CreateJsonResponse(new ComputeNextVersionResponse(message), statusCode);
    }
}