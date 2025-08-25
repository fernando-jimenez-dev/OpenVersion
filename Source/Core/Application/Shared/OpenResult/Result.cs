using OpenResult;
using System.Diagnostics.CodeAnalysis;

namespace Application.Shared.OpenResult;

/// <summary>
/// Represents the outcome of an operation—either success or failure.
/// </summary>
public record Result
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the associated <see cref="OpenResult.Error"/> if the operation failed, or <c>null</c> if successful.
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// Use <see cref="Success"/> or <see cref="Failure"/> instead.
    /// </summary>
    /// <param name="error">The error for a failed result, or <c>null</c> for success.</param>
    private Result(Error? error = null)
    {
        IsSuccess = error is null;
        Error = error;
    }

    /// <summary>
    /// Returns <c>true</c> if the operation was successful.
    /// </summary>
    public bool Succeeded() => IsSuccess;

    /// <summary>
    /// Returns <c>true</c> if the operation failed.
    /// </summary>
    public bool Failed() => IsFailure;

    /// <summary>
    /// Returns <c>true</c> if the operation failed, and outputs the associated error.
    /// </summary>
    /// <param name="error">The error if failed; otherwise <c>null</c>.</param>
    public bool Failed(out Error? error)
    {
        error = Error;
        return IsFailure;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new();

    /// <summary>
    /// Creates a successful generic result with the specified value.
    /// </summary>
    /// <remarks><b>Sugar Syntax</b></remarks>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="value">The value produced by a successful operation.</param>
    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The associated error. Must not be <c>null</c>.</param>
    public static Result Failure(Error error) => new(
        error ?? throw new ArgumentNullException(
            nameof(error),
            "Result.Failure was called with a null error. Every failure must provide a non-null Error instance."
        )
    );

    /// <summary>
    /// Creates a failed generic result with the specified error.
    /// </summary>
    /// <remarks><b>Sugar Syntax</b></remarks>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <param name="error">The associated error. Must not be <c>null</c>.</param>
    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);
}

/// <summary>
/// Represents the outcome of an operation—either success (with a value) or failure (with an error).
/// </summary>
/// <typeparam name="TValue">The result value type.</typeparam>
public record Result<TValue>
{
    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the value produced by a successful operation, or <c>default</c> if failed.
    /// </summary>
    public TValue? Value { get; }

    /// <summary>
    /// Gets the associated <see cref="Error"/> if the operation failed, or <c>null</c> if successful.
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// Returns <c>true</c> if the result has a non-null value. If the operation failed, this will always be <c>false</c>.
    /// If the operation was successful, this will be <c>true</c> if the value is non-null, or <c>false</c> if the value is null.
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{TValue}"/> class.
    /// Use <see cref="Success(TValue)"/> or <see cref="Failure(Error)"/> instead.
    /// </summary>
    /// <param name="value">The result value, or <c>null</c> if empty, or <c>default</c> if failed.</param>
    /// <param name="error">The error if failed, or <c>null</c> if successful.</param>
    private Result(TValue? value = default, Error? error = null)
    {
        IsSuccess = error is null;
        Error = error;
        Value = value;
        HasValue = IsSuccess && value is not null;
    }

    /// <summary>
    /// Returns <c>true</c> if the operation was successful.
    /// </summary>
    public bool Succeeded() => IsSuccess;

    /// <summary>
    /// Returns <c>true</c> if the operation was successful and outputs the value.
    /// </summary>
    /// <param name="value">The result value if successful; otherwise <c>default</c>.</param>
    public bool Succeeded([MaybeNullWhen(true)] out TValue? value)
    {
        value = Value;
        return IsSuccess;
    }

    /// <summary>
    /// Returns <c>true</c> if the operation failed.
    /// </summary>
    public bool Failed() => IsFailure;

    /// <summary>
    /// Returns <c>true</c> if the operation failed, and outputs the associated error.
    /// </summary>
    /// <param name="error">The error if failed; otherwise <c>null</c>.</param>
    public bool Failed(out Error? error)
    {
        error = Error;
        return IsFailure;
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value produced by a successful operation. Must not be <c>null</c>.</param>
    public static Result<TValue> Success(TValue value) => new(
        value ?? throw new ArgumentNullException(
            nameof(value),
            "Result<Value>.Success was called with a null value. Every successful result must provide a non-null value."
        )
    );

    /// <summary>
    /// Creates a successful result that does not have a value.
    /// </summary>
    public static Result<TValue> Success() => new();

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The associated error. Must not be <c>null</c>.</param>
    public static Result<TValue> Failure(Error error) => new(
        default,
        error ?? throw new ArgumentNullException(
            nameof(error),
            "Result<Value>.Failure was called with a null error. Every failure must provide a non-null Error instance."
        )
    );
}