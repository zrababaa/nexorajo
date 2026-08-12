namespace SMPP.Infrastructure.Services;

/// <summary>Bound from the "LinkTracking" config section. BaseUrl blank means link rewriting is
/// disabled: LinkTrackingService leaves messages unmodified rather than emit a broken link.</summary>
public class LinkTrackingOptions
{
    public const string SectionName = "LinkTracking";

    /// <summary>e.g. https://smpp.example.com - no trailing slash. This is the same host the app
    /// is reached at; tracking links are served from GET /l/{token} on this same app.</summary>
    public string BaseUrl { get; set; } = string.Empty;
}
