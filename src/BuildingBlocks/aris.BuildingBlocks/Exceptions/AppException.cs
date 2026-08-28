using Microsoft.AspNetCore.Http;

namespace aris.BuildingBlocks.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string title, string detail, int statusCode, string type)
        : base(detail)
    {
        Title = title;
        StatusCode = statusCode;
        Type = type;
    }

    public string Title { get; }
    public int StatusCode { get; }
    public string Type { get; }
}

public sealed class ValidationAppException : AppException
{
    public ValidationAppException(string detail)
        : base("Validation failed.", detail, StatusCodes.Status400BadRequest, "https://aris.dev/problems/validation-error")
    {
    }
}

public sealed class NotFoundAppException : AppException
{
    public NotFoundAppException(string detail)
        : base("Resource not found.", detail, StatusCodes.Status404NotFound, "https://aris.dev/problems/not-found")
    {
    }
}

public sealed class ConflictAppException : AppException
{
    public ConflictAppException(string detail)
        : base("Conflict.", detail, StatusCodes.Status409Conflict, "https://aris.dev/problems/conflict")
    {
    }
}

public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string detail)
        : base("Invalid credentials.", detail, StatusCodes.Status401Unauthorized, "https://aris.dev/problems/invalid-credentials")
    {
    }
}
