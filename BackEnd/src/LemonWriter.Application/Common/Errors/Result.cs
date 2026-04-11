namespace LemonWriter.Application.Common.Errors;

public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NotFound = new("NOT_FOUND", "The requested resource was not found.");
    public static readonly Error Unauthorized = new("UNAUTHORIZED", "You are not authorized to perform this action.");
    public static readonly Error Validation = new("VALIDATION", "One or more validation errors occurred.");

    public static Error Custom(string code, string message) => new(code, message);
}

public class Result<T>
{
    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result(T value)
    {
        Value = value;
        IsSuccess = true;
        Error = null;
    }

    private Result(Error error)
    {
        Value = default;
        IsSuccess = false;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}

public class Result
{
    public Error? Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    private Result() => IsSuccess = true;
    private Result(Error error)
    {
        IsSuccess = false;
        Error = error;
    }

    public static Result Success() => new();
    public static Result Failure(Error error) => new(error);
}
