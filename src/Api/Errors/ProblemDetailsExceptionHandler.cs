using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Errors;

/// <summary>
/// Global safety net (AD-13): any exception that reaches here — expected <see cref="ApiProblemException"/>
/// or a genuine bug — still comes out as RFC-7807 problem+json with a stable <c>code</c>, never a raw
/// unhandled-exception response.
/// </summary>
public class ProblemDetailsExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // A client disconnect/request cancellation is not an application error — don't report it as one.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        // The response may already be streaming (e.g. the exception happened mid-write) — writing a
        // status code/body at that point throws InvalidOperationException instead of handling cleanly.
        if (httpContext.Response.HasStarted)
        {
            return false;
        }

        var (statusCode, code, title, detail) = exception switch
        {
            ApiProblemException apiEx => (apiEx.StatusCode, apiEx.Code, apiEx.Title, apiEx.Detail),
            _ => (StatusCodes.Status500InternalServerError, "unexpected-error", "An unexpected error occurred.", (string?)null)
        };

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Type = "about:blank",
                Extensions = { ["code"] = code }
            }
        });
    }
}
