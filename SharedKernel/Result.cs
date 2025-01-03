using System.Diagnostics.CodeAnalysis;

namespace SharedKernel;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("Success result can't have an error", nameof(error));
        }

        IsSuccess= isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);
}

public class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    [NotNull]
    public T Value => IsSuccess 
        ? _value! 
        : throw new InvalidOperationException("Value is not available for failed result");

    public static Result<T> Success<T>(T value) => value is not null
        ? new Result<T>(value, true, Error.None)
        : Failure<T>(Error.NullValue);

    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}