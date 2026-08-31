using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SMPP.Application.Common;
using SMPP.Application.UrlShortening;
using SMPP.Infrastructure.Persistence;
using SMPP.Infrastructure.Services;
using Xunit;

namespace SMPP.Tests.Unit;

public class LinkTrackingServiceTests
{
    private const string BaseUrl = "https://sms.example.com";

    private static SmppDbContext BuildDb() =>
        new(new DbContextOptionsBuilder<SmppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IHttpContextAccessor NoRequest() => new HttpContextAccessor();

    private static IHttpContextAccessor RequestOn(string scheme, string host)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Scheme = scheme;
        ctx.Request.Host = new HostString(host);
        return new HttpContextAccessor { HttpContext = ctx };
    }

    private static LinkTrackingService BuildService(
        SmppDbContext db, IUrlShortenerService? shortener = null, string? baseUrl = BaseUrl, IHttpContextAccessor? http = null) =>
        new(db,
            Options.Create(new LinkTrackingOptions { BaseUrl = baseUrl ?? string.Empty }),
            shortener ?? new StubShortener(_ => throw new AppException("shortener off")),
            http ?? NoRequest(),
            NullLogger<LinkTrackingService>.Instance);

    [Fact]
    public async Task PrepareLinkAsync_mints_an_unclaimed_row_and_returns_the_tracking_url()
    {
        using var db = BuildDb();
        var service = BuildService(db);

        var prepared = await service.PrepareLinkAsync("https://dest.example/page", userId: 7, shorten: false);

        Assert.StartsWith($"{BaseUrl}/l/", prepared.TrackingUrl);
        Assert.False(prepared.Shortened);

        var row = Assert.Single(db.TrackedLinks);
        Assert.Equal(string.Empty, row.BatchId);
        Assert.Equal("https://dest.example/page", row.DestinationUrl);
        Assert.Null(row.ShortUrl);
        Assert.Equal(7, row.CreatedByUserId);
    }

    [Fact]
    public async Task PrepareLinkAsync_shortens_the_redirect_when_asked()
    {
        using var db = BuildDb();
        var service = BuildService(db, new StubShortener(_ => "https://sho.rt/abc"));

        var prepared = await service.PrepareLinkAsync("https://dest.example/p", userId: 1, shorten: true);

        Assert.Equal("https://sho.rt/abc", prepared.TrackingUrl);
        Assert.True(prepared.Shortened);
        Assert.Equal("https://sho.rt/abc", Assert.Single(db.TrackedLinks).ShortUrl);
    }

    [Fact]
    public async Task PrepareLinkAsync_falls_back_to_the_full_link_when_shortening_fails()
    {
        using var db = BuildDb();
        var service = BuildService(db, new StubShortener(_ => throw new AppException("Short.io down")));

        var prepared = await service.PrepareLinkAsync("https://dest.example/p", userId: 1, shorten: true);

        Assert.StartsWith($"{BaseUrl}/l/", prepared.TrackingUrl);
        Assert.False(prepared.Shortened);
        Assert.Null(Assert.Single(db.TrackedLinks).ShortUrl);
    }

    [Fact]
    public async Task PrepareLinkAsync_rejects_a_non_http_url()
    {
        using var db = BuildDb();
        var service = BuildService(db);

        await Assert.ThrowsAsync<AppException>(() => service.PrepareLinkAsync("ftp://x/y", 1, false));
        Assert.Empty(db.TrackedLinks);
    }

    [Fact]
    public async Task PrepareLinkAsync_uses_the_request_host_when_BaseUrl_is_not_configured()
    {
        using var db = BuildDb();
        var service = BuildService(db, baseUrl: null, http: RequestOn("http", "85.159.216.122:5000"));

        var prepared = await service.PrepareLinkAsync("https://dest.example/p", userId: 1, shorten: false);

        Assert.StartsWith("http://85.159.216.122:5000/l/", prepared.TrackingUrl);
    }

    [Fact]
    public async Task PrepareLinkAsync_still_throws_when_BaseUrl_is_blank_and_there_is_no_request()
    {
        using var db = BuildDb();
        var service = BuildService(db, baseUrl: null);

        await Assert.ThrowsAsync<AppException>(() => service.PrepareLinkAsync("https://dest.example/p", 1, false));
    }

    [Fact]
    public async Task RewriteMessageAsync_claims_a_prepared_link_without_rewrapping_it()
    {
        using var db = BuildDb();
        var service = BuildService(db);
        var prepared = await service.PrepareLinkAsync("https://dest.example/page", userId: 7, shorten: false);
        var message = $"Hello {prepared.TrackingUrl} bye";

        var rewritten = await service.RewriteMessageAsync(message, "BATCH1", userId: 7);
        await db.SaveChangesAsync();

        Assert.Equal(message, rewritten);
        var row = Assert.Single(db.TrackedLinks);
        Assert.Equal("BATCH1", row.BatchId);
    }

    [Fact]
    public async Task RewriteMessageAsync_claims_a_prepared_shortened_link()
    {
        using var db = BuildDb();
        var service = BuildService(db, new StubShortener(_ => "https://sho.rt/xyz"));
        var prepared = await service.PrepareLinkAsync("https://dest.example/page", userId: 3, shorten: true);
        var message = $"Visit https://sho.rt/xyz today";

        var rewritten = await service.RewriteMessageAsync(message, "BATCH9", userId: 3);
        await db.SaveChangesAsync();

        Assert.Equal(message, rewritten);
        Assert.Equal("BATCH9", Assert.Single(db.TrackedLinks).BatchId);
    }

    [Fact]
    public async Task RewriteMessageAsync_does_not_claim_another_users_prepared_link()
    {
        using var db = BuildDb();
        var service = BuildService(db);
        var prepared = await service.PrepareLinkAsync("https://dest.example/page", userId: 7, shorten: false);

        // A different user sends a message that happens to carry user 7's tracking link.
        var message = $"look {prepared.TrackingUrl}";
        var rewritten = await service.RewriteMessageAsync(message, "BATCH2", userId: 99);
        await db.SaveChangesAsync();

        Assert.Equal(message, rewritten);
        var row = Assert.Single(db.TrackedLinks);
        Assert.Equal(7, row.CreatedByUserId);
        Assert.Equal(string.Empty, row.BatchId);
    }

    [Fact]
    public async Task RewriteMessageAsync_mints_and_rewrites_a_raw_url()
    {
        using var db = BuildDb();
        var service = BuildService(db);

        var rewritten = await service.RewriteMessageAsync("see https://raw.example/x now", "BATCH3", userId: 5);
        await db.SaveChangesAsync();

        Assert.DoesNotContain("raw.example", rewritten);
        Assert.Contains($"{BaseUrl}/l/", rewritten);
        var row = Assert.Single(db.TrackedLinks);
        Assert.Equal("BATCH3", row.BatchId);
        Assert.Equal("https://raw.example/x", row.DestinationUrl);
    }

    private sealed class StubShortener(Func<string, string> map) : IUrlShortenerService
    {
        public Task<string> ShortenUrlAsync(string originalUrl, CancellationToken cancellationToken = default) =>
            Task.FromResult(map(originalUrl));
    }
}
