namespace SharedKernel;

public sealed record Error(string Code, string? Description = null)
{
    public static readonly Error None = new(string.Empty);
    public static readonly Error NullValue = new("Null");

    public static implicit operator Result(Error error) => Result.Failure(error);
}