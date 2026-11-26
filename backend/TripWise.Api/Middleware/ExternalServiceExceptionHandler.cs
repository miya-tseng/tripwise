using Microsoft.AspNetCore.Diagnostics;
using TripWise.Application;
using TripWise.Application.Suggestions;

namespace TripWise.Api.Middleware;

public class ExternalServiceExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ExternalServiceUnavailableException)
        {
            return false;
        }

        var (status, title) = exception is SuggestionsUnavailableException
            ? (StatusCodes.Status503ServiceUnavailable, "Suggestions unavailable")
            : (StatusCodes.Status502BadGateway, "External service unavailable");

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
