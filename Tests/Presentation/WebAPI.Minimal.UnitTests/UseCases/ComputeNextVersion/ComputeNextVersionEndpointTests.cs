using Application.Shared.Errors;
using Application.Shared.OpenResult;
using Application.UseCases.ComputeNextVersion;
using Application.UseCases.ComputeNextVersion.Errors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Net;
using WebAPI.Minimal.UseCases.ComputeNextVersion;

namespace WebAPI.Minimal.UnitTests.UseCases.ComputeNextVersion;

public class ComputeNextVersionEndpointTests
{
    private readonly IComputeNextVersionUseCase useCase;
    private readonly ILogger<ComputeNextVersionEndpoint> logger;
    private readonly CancellationToken cancellationToken;

    public ComputeNextVersionEndpointTests()
    {
        useCase = Substitute.For<IComputeNextVersionUseCase>();
        logger = Substitute.For<ILogger<ComputeNextVersionEndpoint>>();
        cancellationToken = default;
    }

    [Fact]
    public async Task ShouldReturnOkWhenUseCaseSucceeds()
    {
        // Arrange
        var input = new IComputeNextVersionUseCase.Input("main");
        var output = new IComputeNextVersionUseCase.Output("2.0.0.0");
        useCase
            .Run(input, cancellationToken)
            .Returns(Result<IComputeNextVersionUseCase.Output>.Success(output));

        // Act
        var endpointResult = await ComputeNextVersionEndpoint.Execute(useCase, logger, input, cancellationToken);

        // Assert
        var jsonResult = Assert.IsType<JsonHttpResult<ComputeNextVersionResponse>>(endpointResult);
        Assert.Equal((int)HttpStatusCode.OK, jsonResult.StatusCode);
        Assert.NotNull(jsonResult.Value);
        Assert.Equal("2.0.0.0", jsonResult.Value.NextVersion);
    }

    [Fact]
    public async Task ShouldReturnBadRequestWhenValidationErrorOccurs()
    {
        // Arrange
        var input = new IComputeNextVersionUseCase.Input("main");
        var validationError = ValidationError.For(input, "Value of type Input failed validation.");
        useCase
            .Run(input, cancellationToken)
            .Returns(Result<IComputeNextVersionUseCase.Output>.Failure(validationError));

        // Act
        var endpointResult = await ComputeNextVersionEndpoint.Execute(useCase, logger, input, cancellationToken);

        // Assert
        var jsonResult = Assert.IsType<JsonHttpResult<ComputeNextVersionResponse>>(endpointResult);
        Assert.Equal((int)HttpStatusCode.BadRequest, jsonResult.StatusCode);
        Assert.NotNull(jsonResult.Value);
        Assert.Equal("Value of type Input failed validation.", jsonResult.Value.NextVersion);
    }

    [Fact]
    public async Task ShouldReturnBadRequestForUnsupportedBranchError()
    {
        // Arrange
        var input = new IComputeNextVersionUseCase.Input("random-branch");
        var error = new UnsupportedBranchError(input.BranchName);
        useCase
            .Run(input, cancellationToken)
            .Returns(Result<IComputeNextVersionUseCase.Output>.Failure(error));

        // Act
        var endpointResult = await ComputeNextVersionEndpoint.Execute(useCase, logger, input, cancellationToken);

        // Assert
        var jsonResult = Assert.IsType<JsonHttpResult<ComputeNextVersionResponse>>(endpointResult);
        Assert.Equal((int)HttpStatusCode.BadRequest, jsonResult.StatusCode);
        Assert.NotNull(jsonResult.Value);
        Assert.Equal($"No rule was found for rule {input.BranchName}.", jsonResult.Value.NextVersion);
    }

    [Fact]
    public async Task ShouldReturnConflictForVersionConcurrencyError()
    {
        // Arrange
        var input = new IComputeNextVersionUseCase.Input("feature/concurrent");
        var error = new VersionConcurrencyError(new Application.UseCases.ComputeNextVersion.Models.DomainVersion(1, 1, input.BranchName, "1.0.0.1", "meta"));
        useCase
            .Run(input, cancellationToken)
            .Returns(Result<IComputeNextVersionUseCase.Output>.Failure(error));

        // Act
        var endpointResult = await ComputeNextVersionEndpoint.Execute(useCase, logger, input, cancellationToken);

        // Assert
        var jsonResult = Assert.IsType<JsonHttpResult<ComputeNextVersionResponse>>(endpointResult);
        Assert.Equal((int)HttpStatusCode.Conflict, jsonResult.StatusCode);
        Assert.NotNull(jsonResult.Value);
        Assert.Equal(error.Message, jsonResult.Value.NextVersion);
    }

    [Fact]
    public async Task ShouldReturnInternalServerErrorForUnexpectedError()
    {
        // Arrange
        var input = new IComputeNextVersionUseCase.Input("main");
        useCase
            .Run(input, cancellationToken)
            .Returns(Result<IComputeNextVersionUseCase.Output>.Failure(new UnexpectedError("Unexpected issue occurred.", new ApplicationException())));

        // Act
        var endpointResult = await ComputeNextVersionEndpoint.Execute(useCase, logger, input, cancellationToken);

        // Assert
        var statusResult = Assert.IsType<StatusCodeHttpResult>(endpointResult);
        Assert.Equal((int)HttpStatusCode.InternalServerError, statusResult.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnInternalServerErrorForApplicationError()
    {
        // Arrange
        var input = new IComputeNextVersionUseCase.Input("main");
        useCase
            .Run(input, cancellationToken)
            .Returns(Result<IComputeNextVersionUseCase.Output>.Failure(new ApplicationError("SomeType", "Some message.")));

        // Act
        var endpointResult = await ComputeNextVersionEndpoint.Execute(useCase, logger, input, cancellationToken);

        // Assert
        var statusResult = Assert.IsType<StatusCodeHttpResult>(endpointResult);
        Assert.Equal((int)HttpStatusCode.InternalServerError, statusResult.StatusCode);
    }

    [Fact]
    public async Task ShouldReturnInternalServerErrorWhenUseCaseThrowsException()
    {
        // Arrange
        var input = new IComputeNextVersionUseCase.Input("main");
        useCase
            .Run(input, cancellationToken)
            .Throws(new ApplicationException("Critical failure"));

        // Act
        var endpointResult = await ComputeNextVersionEndpoint.Execute(useCase, logger, input, cancellationToken);

        // Assert
        var jsonResult = Assert.IsType<JsonHttpResult<ComputeNextVersionResponse>>(endpointResult);
        Assert.Equal((int)HttpStatusCode.InternalServerError, jsonResult.StatusCode);
        Assert.NotNull(jsonResult.Value);
        Assert.Equal("Unrecoverable error encountered.", jsonResult.Value.NextVersion);
    }
}