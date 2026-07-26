using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class SpamKeywordFilterService : ISpamKeywordFilterService
{
    private readonly SmppDbContext _db;
    private readonly ISmsGatewayClient _gateway;

    public SpamKeywordFilterService(SmppDbContext db, ISmsGatewayClient gateway)
    {
        _db = db;
        _gateway = gateway;
    }

    public async Task<SpamFilterResult> CheckAsync(string message, CancellationToken ct = default)
    {
        var include = await _db.SpamKeywords.Where(k => k.KeywordType == SpamKeywordType.Include).Select(k => k.Keyword).ToListAsync(ct);
        var exclude = await _db.SpamKeywords.Where(k => k.KeywordType == SpamKeywordType.Exclude).Select(k => k.Keyword).ToListAsync(ct);
        var urls = await _db.SpamKeywords.Where(k => k.KeywordType == SpamKeywordType.Url).Select(k => k.Keyword).ToListAsync(ct);

        return await _gateway.PreflightFilterAsync(new SpamFilterRequest(message, include, exclude, urls), ct);
    }
}
