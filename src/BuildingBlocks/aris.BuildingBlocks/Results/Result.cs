namespace aris.BuildingBlocks.Results;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("A successful result cannot have an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("A failed result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure
    {
        get { return !IsSuccess; }
    }

    public Error Error { get; }

    public static Result Success()
    {
        return new(true, Error.None);
    }

    public static Result Failure(Error error)
    {
        return new(false, error);
    }

    public static Result<TValue> Success<TValue>(TValue value)
    {
        return new(value, true, Error.None);
    }

    public static Result<TValue> Failure<TValue>(Error error)
    {
        return new(default, false, error);
    }
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value
    {
        get
        {
            return IsSuccess
                ? _value!
                : throw new InvalidOperationException("The value of a failed result cannot be accessed.");
        }
    }

    public static implicit operator Result<TValue>(TValue value)
    {
        return Success(value);
    }
}
