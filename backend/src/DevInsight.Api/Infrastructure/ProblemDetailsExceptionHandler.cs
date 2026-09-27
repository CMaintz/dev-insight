using DevInsight.Application.Common;
using DevInsight.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DevInsight.Api.Infrastructure;

/// <summary>Maps application and domain exceptions to RFC 9457 problem details.</summary>
internal sealed class ProblemDetailsExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            DomainException => (StatusCodes.Status400BadRequest, "Invalid request"),
            BadHttpRequestException bad => (bad.StatusCode, "Bad request"),
            PreconditionFailedException => (StatusCodes.Status412PreconditionFailed, "Precondition failed"),
            _ => (0, string.Empty),
        };
        if (status == 0)
        {
            return false;
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = exception.Message },
        });
    }
}
