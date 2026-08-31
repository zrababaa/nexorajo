namespace SMPP.Infrastructure.Services;

/// <summary>
/// Bound from the "ShortIo" configuration section. Both values must be set for URL shortening to
/// work: when either is blank <see cref="ShortIoUrlShortenerService"/> throws (so the standalone
/// endpoint returns 400) and the send path falls back to the full tracking link.
///
/// Keep <see cref="ApiKey"/> out of source control - it belongs in appsettings.Development.json
/// for local dev and appsettings.Production.json (git-ignored) in production, not appsettings.json.
/// </summary>
public class ShortIoOptions
{
    public const string SectionName = "ShortIo";

    /// <summary>
    /// Short.io API key (Short.io dashboard -&gt; Settings -&gt; Integrations &amp; API). Sent verbatim
    /// in the Authorization header - never logged, never echoed in an error.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The short domain links are created under, e.g. <c>yourbrand.short.gy</c>
    /// (Short.io dashboard -&gt; Domains).
    /// </summary>
    public string Domain { get; set; } = string.Empty;
}
