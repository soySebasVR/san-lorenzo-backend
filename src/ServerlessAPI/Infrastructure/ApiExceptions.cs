namespace ServerlessAPI.Infrastructure;

/// <summary>Domain error that ApiExceptionHandler turns into an HTTP response.</summary>
public abstract class ApiException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }
    public abstract string Title { get; }
}

public sealed class UnauthorizedException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status401Unauthorized;
    public override string Title => "Not authenticated";
}

/// <summary>The resource exists but does not belong to the caller.</summary>
public sealed class ForbiddenException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status403Forbidden;
    public override string Title => "Access denied";
}

public sealed class NotFoundException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status404NotFound;
    public override string Title => "Not found";
}
