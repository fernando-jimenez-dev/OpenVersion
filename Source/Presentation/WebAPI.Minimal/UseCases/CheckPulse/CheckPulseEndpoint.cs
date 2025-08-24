using Application.Shared.Errors;
using Application.UseCases.CheckPulse.Abstractions;
using Application.UseCases.CheckPulse.Errors;
using Microsoft.AspNetCore.Mvc;
using OpenResult;
using System.Net;

namespace WebAPI.Minimal.UseCases.CheckPulse;

public class CheckPulseEndpoint
{
    public static async Task<IResult> Execute(
        [FromServices] ICheckPulseUseCase checkPulseUseCase,
        [FromServices] ILogger<CheckPulseEndpoint> logger,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var input = "Default use case input.";
            var output = await checkPulseUseCase.Run(input, cancellationToken);

            return output.IsSuccess
                ? HandleSuccess(logger)
                : HandleError(output.Error, logger);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "There was an unrecoverable error while pulse checking.");
            return CreateErrorResponse("Unrecoverable error encountered.", HttpStatusCode.InternalServerError);
        }
    }

    private static IResult HandleSuccess(ILogger<CheckPulseEndpoint> logger)
    {
        logger.LogInformation("Pulse checked!");
        return CreateJsonResponse(new CheckPulseEndpointResponse("Pulse checked!"), HttpStatusCode.OK);
    }

    private static IResult HandleError(Error error, ILogger<CheckPulseEndpoint> logger)
    {
        switch (error)
        {
            case ValidationError<string> validationError:
                return CreateJsonResponse(new CheckPulseEndpointResponse(validationError.Message), HttpStatusCode.BadRequest);

            case EmptyVitalsError emptyVitalsError:
                logger.LogWarning("Empty vitals: {}", emptyVitalsError.Message);
                return CreateJsonResponse(new CheckPulseEndpointResponse(emptyVitalsError.Message), HttpStatusCode.InternalServerError);

            case UnexpectedError unexpectedError:
                logger.LogError("Pulse check failed with message {}", unexpectedError.Message);
                return Results.StatusCode((int)HttpStatusCode.InternalServerError);

            default:
                logger.LogError("An unknown error occurred while running the Check Pulse use case. Message: '{}'", error?.Message ?? string.Empty);
                return Results.StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }

    private static IResult CreateJsonResponse(CheckPulseEndpointResponse response, HttpStatusCode statusCode)
    {
        return Results.Json(data: response, statusCode: (int)statusCode);
    }

    private static IResult CreateErrorResponse(string message, HttpStatusCode statusCode)
    {
        return CreateJsonResponse(new CheckPulseEndpointResponse(message), statusCode);
    }
}