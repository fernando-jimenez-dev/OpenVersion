using Application.Shared.Errors;
using Application.UseCases.CheckPulse;
using Application.UseCases.CheckPulse.Abstractions;
using Application.UseCases.CheckPulse.Errors;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Application.UnitTests.UseCases.CheckPulse;

public class CheckPulseUseCaseTests
{
    private readonly ILogger<CheckPulseUseCase> logger;
    private readonly ICheckPulseRepository checkPulseRepository;
    private readonly CheckPulseUseCase checkPulseUseCase;

    public CheckPulseUseCaseTests()
    {
        logger = Substitute.For<ILogger<CheckPulseUseCase>>();
        checkPulseRepository = Substitute.For<ICheckPulseRepository>();
        checkPulseUseCase = new CheckPulseUseCase(logger, checkPulseRepository);
    }

    [Fact]
    public async Task ShouldSucceedWhenVitalsAreGood()
    {
        // Arrange
        string[] goodVitals = ["All", "Good!"];
        checkPulseRepository.RetrieveVitalReadings(Arg.Any<CancellationToken>()).Returns(goodVitals);

        // Act
        var input = "Test message";
        var useCaseOutput = await checkPulseUseCase.Run(input);

        // Assert
        Assert.True(useCaseOutput.IsSuccess);
        Assert.Null(useCaseOutput.Error);
        await checkPulseRepository.Received(1).SaveNewVitalCheck();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ShouldFailWhenInputIsInvalid(string invalidInput)
    {
        // Act
        var useCaseOutput = await checkPulseUseCase.Run(invalidInput);

        // Assert
        Assert.False(useCaseOutput.IsSuccess);
        var validationError = Assert.IsType<ValidationError<string>>(useCaseOutput.Error);
        Assert.Equal("Value of type String failed validation.", validationError.Message);
        await checkPulseRepository.DidNotReceive().SaveNewVitalCheck();
    }

    [Fact]
    public async Task ShouldFailWhenVitalsAreEmpty()
    {
        // Arrange
        string[] emptyVitals = [];
        checkPulseRepository.RetrieveVitalReadings(Arg.Any<CancellationToken>()).Returns(emptyVitals);

        // Act
        var input = "Test message";
        var useCaseOutput = await checkPulseUseCase.Run(input);

        // Assert
        Assert.False(useCaseOutput.IsSuccess);
        Assert.IsType<EmptyVitalsError>(useCaseOutput.Error);
        await checkPulseRepository.DidNotReceive().SaveNewVitalCheck();
    }

    [Fact]
    public async Task ShouldHandleUnexpectedException()
    {
        // Arrange
        var exception = new Exception("Something went wrong!");
        checkPulseRepository.RetrieveVitalReadings(Arg.Any<CancellationToken>()).Throws(exception);

        // Act
        var input = "Test message";
        var useCaseOutput = await checkPulseUseCase.Run(input);

        // Assert
        Assert.False(useCaseOutput.IsSuccess);
        var unexpectedError = Assert.IsType<UnexpectedError>(useCaseOutput.Error);
        Assert.Equal($"Unexpected error during Check Pulse use case. Input: '{input}'", unexpectedError.Message);
        Assert.Equal(exception, unexpectedError.Exception);
        await checkPulseRepository.DidNotReceive().SaveNewVitalCheck();
    }
}