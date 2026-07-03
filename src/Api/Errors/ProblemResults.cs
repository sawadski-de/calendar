namespace Api.Errors;

/// <summary>
/// Uniform helper so every expected-error endpoint response is RFC-7807 <c>application/problem+json</c>
/// with a stable machine-readable <c>code</c> field (AD-13) — never localized free text.
/// </summary>
public static class ProblemResults
{
    public static IResult Problem(int statusCode, string code, string title, string? detail = null) =>
        Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: detail,
            type: "about:blank",
            extensions: new Dictionary<string, object?> { ["code"] = code });
}
