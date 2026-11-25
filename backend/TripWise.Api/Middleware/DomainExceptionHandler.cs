using Microsoft.AspNetCore.Diagnostics;
using TripWise.Api.Domain;

namespace TripWise.Api.Middleware;

public class DomainExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException)
        {
            return false;
        }

        var (status, title) = exception switch
        {
            ActivityOverlapException => (StatusCodes.Status409Conflict, "Activity overlap"),
            ActivityNotFoundException => (StatusCodes.Status404NotFound, "Activity not found"),
            _ => (StatusCodes.Status400BadRequest, "Business rule violation"),
        };

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Title = title,
                Detail = exception.Message,
                Status = status,
            },
        });
    }
}
