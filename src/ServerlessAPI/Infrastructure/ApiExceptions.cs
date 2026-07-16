namespace ServerlessAPI.Infrastructure;

/// <summary>Excepción de dominio manejada por el API.</summary>
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

/// <summary>Acceso denegado al recurso.</summary>
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

public sealed class ConflictException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status409Conflict;
    public override string Title => "Conflict";
}

public sealed class BadRequestException(string message) : ApiException(message)
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public override string Title => "Invalid request";
}
