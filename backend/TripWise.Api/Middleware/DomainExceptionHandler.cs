using Microsoft.AspNetCore.Diagnostics;
using TripWise.Domain;

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
            TripDatesConflictException => (StatusCodes.Status409Conflict, "Trip dates conflict"),
            ActivityNotFoundException => (StatusCodes.Status404NotFound, "Activity not found"),
            StayOverlapException => (StatusCodes.Status409Conflict, "Stay overlap"),
            StayNotFoundException => (StatusCodes.Status404NotFound, "Stay not found"),
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
