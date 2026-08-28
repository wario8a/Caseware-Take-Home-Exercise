namespace Collaborate.Api.Common;

public enum ResultStatus
{
    Success,
    NotFound,
    Invalid,
    Failure
}

public sealed class Result<T>
{
    private Result(ResultStatus status, T? value = default, string? error = null)
    {
        Status = status;
        Value = value;
        Error = error;
    }

    public ResultStatus Status { get; }

    public T? Value { get; }

    public string? Error { get; }

    public bool IsSuccess => Status == ResultStatus.Success;

    public static Result<T> Success(T value) => new(ResultStatus.Success, value);

    public static Result<T> NotFound(string? error = null) => new(ResultStatus.NotFound, default, error);

    public static Result<T> Invalid(string error) => new(ResultStatus.Invalid, default, error);

    public static Result<T> Failure(string error) => new(ResultStatus.Failure, default, error);
}
