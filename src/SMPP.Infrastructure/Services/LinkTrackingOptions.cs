namespace SMPP.Infrastructure.Services;

/// <summary>Bound from the "LinkTracking" config section. When BaseUrl is blank,
/// LinkTrackingService falls back to the current request's own scheme+host, so tracking still
/// works with no config for a normal (request-bound) send; it is only skipped when there is also
/// no request, e.g. a scheduled send on a server that never set BaseUrl.</summary>
public class LinkTrackingOptions
{
    public const string SectionName = "LinkTracking";

    /// <summary>e.g. https://smpp.example.com - no trailing slash. This is the same host the app
    /// is reached at; tracking links are served from GET /l/{token} on this same app. Leave blank
    /// to use whatever host the request came in on; set it to pin links to a fixed public host
    /// (and to have scheduled/background sends tracked too).</summary>
    public string BaseUrl { get; set; } = string.Empty;
}
