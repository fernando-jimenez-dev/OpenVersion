using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Application.Shared.Errors;
using Application.Shared.OpenResult;
using Application.UseCases.GetProjectVersions;
using Application.UseCases.GetProjectVersions.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using WebAPI.Minimal.UseCases.GetProjectVersions;

namespace WebAPI.Minimal.UnitTests.UseCases.GetProjectVersions;

public class GetProjectVersionsEndpointTests
{
    private readonly IGetProjectVersionsUseCase _useCase;
    private readonly ILogger<GetProjectVersionsEndpoint> _logger;
    private readonly CancellationToken _cancellationToken;

    public GetProjectVersionsEndpointTests()
    {
        _useCase = Substitute.For<IGetProjectVersionsUseCase>();
        _logger = Substitute.For<ILogger<GetProjectVersionsEndpoint>>();
        _cancellationToken = default;
    }

    [Fact]
    public async Task ShouldReturnOkWhenUseCaseSucceeds()
    {
        // Arrange
        var projectId = 5L;
        var versions = new List<ProjectVersion>
        {
            new(1, projectId, "main", "1.0.0.0", null)
        };
        var output = new IGetProjectVersionsUseCase.Output(projectId, versions);
        _useCase
            .Run(Arg.Is<IGetProjectVersionsUseCase.Input>(input => input.ProjectId == projectId), _cancellationToken)
            .Returns(Result<IGetProjectVersionsUseCase.Output>.Success(output));

        // Act
        var endpointResult = await GetProjectVersionsEndpoint.Execute(_useCase, _logger, projectId, _cancellationToken);

        // Assert
        var jsonResult = Assert.IsType<JsonHttpResult<GetProjectVersionsResponse>>(endpointResult);
        Assert.Equal((int)HttpStatusCode.OK, jsonResult.StatusCode);
        Assert.NotNull(jsonResult.Value);
        Assert.Equal(projectId, jsonResult.Value.ProjectId);
        Assert.Single(jsonResult.Value.Versions);
        Assert.Equal("main", jsonResult.Value.Versions[0].IdentifierName);
    }

    [Fact]
    public async Task ShouldReturnBadRequestWhenValidationErrorOccurs()
    {
        // Arrange
        var projectId = 3L;
        var input = new IGetProjectVersionsUseCase.Input(projectId);
        var validationError = ValidationError.For(input, "Invalid project");
        _useCase
            .Run(input, _cancellationToken)
            .Returns(Result<IGetProjectVersionsUseCase.Output>.Failure(validationError));

        // Act
        var endpointResult = await GetProjectVersionsEndpoint.Execute(_useCase, _logger, projectId, _cancellationToken);

        // Assert
        var jsonResult = Assert.IsType<JsonHttpResult<GetProjectVersionsErrorResponse>>(endpointResult);
        Assert.Equal((int)HttpStatusCode.BadRequest, jsonResult.StatusCode);
        Assert.NotNull(jsonResult.Value);
        Assert.Equal("Invalid project", jsonResult.Value.Message);
    }

    [Fact]
    public async Task ShouldReturnInternalServerErrorForUnexpectedError()
    {
        // Arrange
        var projectId = 4L;
        _useCase
            .Run(Arg.Is<IGetProjectVersionsUseCase.Input>(input => input.ProjectId == projectId), _cancellationToken)
            .Returns(Result<IGetProjectVersionsUseCase.Output>.Failure(new UnexpectedError("boom", new ApplicationException())));

        // Act
        var endpointResult = await GetProjectVersionsEndpoint.Execute(_useCase, _logger, projectId, _cancellationToken);

        // Assert
        var statusResult = Assert.IsType<StatusCodeHttpResult>(endpointResult);
        Assert.Equal((int)HttpStatusCode.InternalServerError, statusResult.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnInternalServerErrorForApplicationError()
    {
        // Arrange
        var projectId = 8L;
        _useCase
            .Run(Arg.Is<IGetProjectVersionsUseCase.Input>(input => input.ProjectId == projectId), _cancellationToken)
            .Returns(Result<IGetProjectVersionsUseCase.Output>.Failure(new ApplicationError("Any", "broken")));

        // Act
        var endpointResult = await GetProjectVersionsEndpoint.Execute(_useCase, _logger, projectId, _cancellationToken);

        // Assert
        var statusResult = Assert.IsType<StatusCodeHttpResult>(endpointResult);
        Assert.Equal((int)HttpStatusCode.InternalServerError, statusResult.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnInternalServerErrorWhenUseCaseThrowsException()
    {
        // Arrange
        var projectId = 9L;
        _useCase
            .Run(Arg.Any<IGetProjectVersionsUseCase.Input>(), _cancellationToken)
            .Throws(new ApplicationException("Critical failure"));

        // Act
        var endpointResult = await GetProjectVersionsEndpoint.Execute(_useCase, _logger, projectId, _cancellationToken);

        // Assert
        var jsonResult = Assert.IsType<JsonHttpResult<GetProjectVersionsErrorResponse>>(endpointResult);
        Assert.Equal((int)HttpStatusCode.InternalServerError, jsonResult.StatusCode);
        Assert.NotNull(jsonResult.Value);
        Assert.Equal("Unrecoverable error encountered.", jsonResult.Value.Message);
    }
}
