namespace SMPP.Application.UrlShortening;

/// <summary>
/// Turns a long URL into a short one via an external shortening provider. The send path uses this
/// (behind the per-send "shorten links" choice) to wrap a tracking link in a compact form, and
/// <c>POST /api/v1/url-shortener</c> exposes it directly.
///
/// Contracts only - the Short.io-backed implementation and its options live in
/// SMPP.Infrastructure, registered as a typed <see cref="System.Net.Http.HttpClient"/> in
/// DependencyInjection.AddInfrastructure.
/// </summary>
public interface IUrlShortenerService
{
    /// <summary>Returns the shortened form of <paramref name="originalUrl"/>.</summary>
    /// <exception cref="SMPP.Application.Common.AppException">
    /// <paramref name="originalUrl"/> is empty or not an absolute http(s) URL, the provider is not
    /// configured, or the provider rejected the request or could not be reached.
    /// </exception>
    Task<string> ShortenUrlAsync(string originalUrl, CancellationToken cancellationToken = default);
}
