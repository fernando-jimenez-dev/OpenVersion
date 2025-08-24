using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Application.Shared.Errors;

/// <summary>
/// Generic validation error that exposes the value that failed validation,
/// as well as the validation errors that were found.
/// </summary>
/// <remarks>
/// It has a severity of <see cref="LogLevel.Warning"/> because validation is an expected error that requires follow-up from the consumer.
/// </remarks>
/// <typeparam name="TValue">C# Type that failed validation.</typeparam>
public record ValidationError<TValue> : ApplicationError
{
    public TValue Value { get; }
    public IReadOnlyList<string> Issues { get; }
    public override LogLevel Severity => LogLevel.Warning;

    public static string Name = BuildErrorType();

    // Primary constructor: accepts any issue list
    public ValidationError(TValue value, IEnumerable<string> issues)
        : base(Name, $"Value of type {GetValueTypeName()} failed validation.")
    {
        Value = value;
        Issues = issues?.ToList() ?? [];
    }

    // Convenience constructor for single issue
    public ValidationError(TValue value, string issue) : this(value, [issue]) { }

    // Convenience constructor for ValidationResult
    public ValidationError(TValue value, ValidationResult validationResult)
        : this(value, ExtractIssues(validationResult)) { }

    private static string BuildErrorType() => $"ValidationError<{GetValueTypeName()}>";

    private static string GetValueTypeName() => typeof(TValue).Name;

    private static IEnumerable<string> ExtractIssues(ValidationResult validationResult)
        => validationResult is null
            ? []
            : validationResult.Errors.Select(e => e.ErrorMessage);
}

public static class ValidationError
{
    public static ValidationError<TInput> For<TInput>(TInput input, IEnumerable<string> issues)
       => new(input, issues);

    public static ValidationError<TInput> For<TInput>(TInput input, string issue)
       => new(input, issue);

    public static ValidationError<TInput> For<TInput>(TInput input, ValidationResult validationResult)
       => new(input, validationResult);
}