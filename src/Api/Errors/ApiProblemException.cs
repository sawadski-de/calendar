namespace Api.Errors;

/// <summary>
/// Thrown for unexpected failures that should still surface as RFC-7807 problem+json with a stable
/// <c>code</c> (AD-13) rather than an unhandled-exception response. Expected/control-flow error cases
/// (invalid credentials, last-admin protection) return <see cref="ProblemResults.Problem"/> directly
/// from the endpoint instead of throwing.
/// </summary>
public class ApiProblemException(int statusCode, string code, string title, string? detail = null)
    : Exception(detail ?? title)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
    public string Title { get; } = title;
    public string? Detail { get; } = detail;
}
