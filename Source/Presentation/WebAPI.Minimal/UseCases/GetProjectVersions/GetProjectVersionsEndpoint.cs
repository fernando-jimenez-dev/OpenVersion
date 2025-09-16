using System;
using System.Linq;
using System.Net;
using Application.Shared.Errors;
using Application.UseCases.GetProjectVersions;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Minimal.UseCases.GetProjectVersions;

public static class GetProjectVersionsEndpoint
{
    public static async Task<IResult> Execute(
        [FromServices] IGetProjectVersionsUseCase getProjectVersionsUseCase,
        [FromServices] ILogger<GetProjectVersionsEndpoint> logger,
        [FromRoute] long projectId,
        CancellationToken cancellationToken)
    {
        try
        {
            var input = new IGetProjectVersionsUseCase.Input(projectId);
            var result = await getProjectVersionsUseCase.Run(input, cancellationToken);

            return result.IsSuccess
                ? HandleSuccess(result.Value!, logger)
                : HandleError(result.Error!, logger, projectId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "There was an unrecoverable error while retrieving project versions.");
            return CreateErrorResponse("Unrecoverable error encountered.", HttpStatusCode.InternalServerError);
        }
    }

    private static IResult HandleSuccess(IGetProjectVersionsUseCase.Output output, ILogger<GetProjectVersionsEndpoint> logger)
    {
        logger.LogInformation("Retrieved {Count} versions for project {ProjectId}", output.Versions.Count, output.ProjectId);

        var response = new GetProjectVersionsResponse(
            output.ProjectId,
            output.Versions
                .Select(version => new GetProjectVersionsResponse.ProjectVersionItem(
                    version.Id,
                    version.IdentifierName,
                    version.ReleaseNumber,
                    version.Meta))
                .ToArray());

        return CreateJsonResponse(response, HttpStatusCode.OK);
    }

    private static IResult HandleError(Error error, ILogger<GetProjectVersionsEndpoint> logger, long projectId)
    {
        switch (error)
        {
            case ValidationError<IGetProjectVersionsUseCase.Input> validationError:
                logger.LogWarning(
                    "Invalid input for get project versions for project {ProjectId}: {Message}",
                    projectId,
                    validationError.Message);
                return CreateJsonResponse(
                    new GetProjectVersionsErrorResponse(validationError.Message),
                    HttpStatusCode.BadRequest);

            case UnexpectedError unexpectedError:
                logger.LogError(
                    unexpectedError.Exception,
                    "Unexpected error retrieving project versions for project {ProjectId}: {Message}",
                    projectId,
                    unexpectedError.Message);
                return Results.StatusCode((int)HttpStatusCode.InternalServerError);

            case ApplicationError appError:
                logger.Log(
                    appError.Severity,
                    appError.Exception,
                    "Application error retrieving project versions for project {ProjectId}: {Type} - {Message}",
                    projectId,
                    appError.Type,
                    appError.Message);
                return Results.StatusCode((int)HttpStatusCode.InternalServerError);

            default:
                logger.LogError(
                    "An unknown error occurred while running the Get Project Versions use case for project {ProjectId}. Message: '{Message}'",
                    projectId,
                    error?.Message ?? string.Empty);
                return Results.StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }

    private static IResult CreateJsonResponse<TResponse>(TResponse response, HttpStatusCode statusCode)
    {
        return Results.Json(data: response, statusCode: (int)statusCode);
    }

    private static IResult CreateErrorResponse(string message, HttpStatusCode statusCode)
    {
        return CreateJsonResponse(new GetProjectVersionsErrorResponse(message), statusCode);
    }
}
