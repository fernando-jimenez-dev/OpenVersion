using Application.Shared.OpenResult;

namespace Application.Shared.Abstractions.UseCase
{
    /// <summary>
    /// Represents a use case that performs an action.
    /// </summary>
    public interface IUseCase
    {
        /// <summary>
        /// Executes the use case.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<Result> Run(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a use case that takes an input, performs an action, and returns an output.
    /// </summary>
    /// <typeparam name="TUseCaseInput">The type of the use case input.</typeparam>
    /// <typeparam name="TUseCaseOutput">The type of the use case output.</typeparam>
    public interface IUseCase<in TUseCaseInput, TUseCaseOutput>
    {
        /// <summary>
        /// Executes the use case.
        /// </summary>
        /// <param name="input">The input for the use case.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the execution output.</returns>
        Task<Result<TUseCaseOutput>> Run(TUseCaseInput input, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a use case that takes an input and performs an action.
    /// </summary>
    /// <typeparam name="TUseCaseInput">The type of the use case input.</typeparam>
    public interface IUseCase<in TUseCaseInput>
    {
        /// <summary>
        /// Executes the use case.
        /// </summary>
        /// <param name="input">The input for the use case.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<Result> Run(TUseCaseInput input, CancellationToken cancellationToken = default);
    }
}

namespace Application.Shared.Abstractions.UseCase.OutputOnly
{
    /// <summary>
    /// Represents a use case that performs an action and returns an output.
    /// </summary>
    /// <typeparam name="TUseCaseOutput">The type of the use case output.</typeparam>
    public interface IUseCase<TUseCaseOutput>
    {
        /// <summary>
        /// Executes the use case.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the execution output.</returns>
        Task<Result<TUseCaseOutput>> Run(CancellationToken cancellationToken = default);
    }
}