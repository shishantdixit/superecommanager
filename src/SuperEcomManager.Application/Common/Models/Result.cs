namespace SuperEcomManager.Application.Common.Models;

/// <summary>
/// Represents the result of an operation.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string[] Errors { get; }
    public string[] Warnings { get; protected set; } = Array.Empty<string>();
    public bool HasWarnings => Warnings.Length > 0;

    protected Result(bool isSuccess, string[] errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Success() => new(true, Array.Empty<string>());
    public static Result Failure(params string[] errors) => new(false, errors);
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(params string[] errors) => Result<T>.Failure(errors);
}

/// <summary>
/// Represents the result of an operation with a value.
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string[] errors, string[]? warnings = null)
        : base(isSuccess, errors)
    {
        Value = value;
        Warnings = warnings ?? Array.Empty<string>();
    }

    public static Result<T> Success(T value) => new(true, value, Array.Empty<string>());
    public new static Result<T> Failure(params string[] errors) => new(false, default, errors);

    /// <summary>
    /// Creates a success result with warning messages.
    /// The operation succeeded, but there are warnings to display to the user.
    /// </summary>
    public static Result<T> SuccessWithWarning(T value, params string[] warnings) =>
        new(true, value, Array.Empty<string>(), warnings);

    public static implicit operator Result<T>(T value) => Success(value);
}
