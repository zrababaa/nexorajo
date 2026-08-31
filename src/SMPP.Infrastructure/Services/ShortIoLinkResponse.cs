using System.Text.Json.Serialization;

namespace SMPP.Infrastructure.Services;

/// <summary>
/// The subset of Short.io's <c>POST /links</c> response the app reads. Short.io returns many more
/// fields (id, path, title, OpenGraph tags, ...); only the generated short URL is needed here.
/// <see cref="SecureShortUrl"/> is the https form of <see cref="ShortUrl"/> and is preferred when
/// present.
/// </summary>
public sealed class ShortIoLinkResponse
{
    [JsonPropertyName("shortURL")]
    public string? ShortUrl { get; set; }

    [JsonPropertyName("secureShortURL")]
    public string? SecureShortUrl { get; set; }

    [JsonPropertyName("idString")]
    public string? IdString { get; set; }

    [JsonPropertyName("originalURL")]
    public string? OriginalUrl { get; set; }
}
