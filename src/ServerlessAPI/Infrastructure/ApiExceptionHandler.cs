using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ServerlessAPI.Infrastructure;

/// <summary>Mapea errores de dominio a HTTP.</summary>
public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ApiException apiException)
        {
            logger.LogError(exception, "Unhandled error at {Path}", httpContext.Request.Path);
            return false;
        }

        logger.LogWarning(
            "Request rejected with {StatusCode} at {Path}: {Reason}",
            apiException.StatusCode, httpContext.Request.Path, apiException.Message);

        var problem = new ProblemDetails
        {
            Status = apiException.StatusCode,
            Title = apiException.Title,
            Detail = apiException.Message,
            Instance = httpContext.Request.Path,
        };

        httpContext.Response.StatusCode = apiException.StatusCode;
        await httpContext.Response
            .WriteAsJsonAsync(problem, cancellationToken)
            .ConfigureAwait(false);

        return true;
    }
}
