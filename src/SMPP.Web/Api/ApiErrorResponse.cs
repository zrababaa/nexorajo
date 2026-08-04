namespace SMPP.Web.Api;

/// <summary>
/// The one error shape every <c>/api/v1</c> endpoint returns, whatever went wrong - a rejected
/// business rule, a validation failure, or a blocked message - so a client can parse failures
/// with a single model.
/// </summary>
public class ApiErrorResponse
{
    public ApiErrorResponse(string message)
    {
        Message = message;
    }

    /// <summary>Human-readable description of the failure.</summary>
    public string Message { get; }

    /// <summary>Field-level validation messages, keyed by property name. Null unless the request failed validation.</summary>
    public IDictionary<string, string[]>? Errors { get; init; }

    /// <summary>The terms the content filter matched. Only set on a 422 from a send endpoint.</summary>
    public IReadOnlyCollection<string>? BlockedTerms { get; init; }
}
