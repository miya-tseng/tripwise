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

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Title = "Business rule violation",
                Detail = exception.Message,
                Status = StatusCodes.Status400BadRequest,
            },
        });
    }
}
