using Microsoft.AspNetCore.Diagnostics;
using TripWise.Application;

namespace TripWise.Api.Middleware;

public class ExternalServiceExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ExternalServiceUnavailableException)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Title = "External service unavailable",
                Detail = exception.Message,
                Status = StatusCodes.Status502BadGateway,
            },
        });
    }
}
