using Microsoft.EntityFrameworkCore;
using SMPP.Application.Campaigns;
using SMPP.Application.Common;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class CampaignService : ICampaignService
{
    private readonly SmppDbContext _db;

    public CampaignService(SmppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<CampaignListItemDto>> GetPagedAsync(int ownerUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Campaigns.Where(c => c.CreatedByUserId == ownerUserId).OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CampaignListItemDto(
                c.Id,
                c.Name,
                c.ExternalCampaignCode,
                c.Numbers.Length == 0 ? 0 : c.Numbers.Split(',', StringSplitOptions.RemoveEmptyEntries).Length,
                c.SourceType,
                c.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<CampaignListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task<CampaignDetailDto?> GetByIdAsync(int id, int ownerUserId, CancellationToken ct = default)
    {
        var campaign = await FindOwnedAsync(id, ownerUserId, ct);
        return campaign is null ? null : ToDetailDto(campaign);
    }

    public async Task<int> CreateAsync(int ownerUserId, CreateCampaignRequest request, CancellationToken ct = default)
    {
        var codeInUse = await _db.Campaigns.AnyAsync(c => c.ExternalCampaignCode == request.ExternalCampaignCode, ct);
        if (codeInUse)
        {
            throw new AppException($"Campaign code '{request.ExternalCampaignCode}' is already in use.");
        }

        var campaign = new Campaign
        {
            Name = request.Name,
            ExternalCampaignCode = request.ExternalCampaignCode,
            Numbers = request.NormalizedNumbers,
            SourceType = request.SourceType,
            SendSpeedMinSeconds = request.SendSpeedMinSeconds,
            SendSpeedMaxSeconds = request.SendSpeedMaxSeconds,
            CreatedByUserId = ownerUserId,
        };

        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync(ct);
        return campaign.Id;
    }

    public async Task UpdateAsync(int id, int ownerUserId, UpdateCampaignRequest request, CancellationToken ct = default)
    {
        var campaign = await FindOwnedAsync(id, ownerUserId, ct)
            ?? throw new AppException("Campaign not found.");

        campaign.Name = request.Name;
        campaign.Numbers = request.NormalizedNumbers;
        campaign.SendSpeedMinSeconds = request.SendSpeedMinSeconds;
        campaign.SendSpeedMaxSeconds = request.SendSpeedMaxSeconds;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, int ownerUserId, CancellationToken ct = default)
    {
        var campaign = await FindOwnedAsync(id, ownerUserId, ct)
            ?? throw new AppException("Campaign not found.");

        _db.Campaigns.Remove(campaign);
        await _db.SaveChangesAsync(ct);
    }

    private Task<Campaign?> FindOwnedAsync(int id, int ownerUserId, CancellationToken ct) =>
        _db.Campaigns.FirstOrDefaultAsync(c => c.Id == id && c.CreatedByUserId == ownerUserId, ct);

    private static CampaignDetailDto ToDetailDto(Campaign c) => new(
        c.Id,
        c.Name,
        c.ExternalCampaignCode,
        c.Numbers,
        c.Numbers.Length == 0 ? 0 : c.Numbers.Split(',', StringSplitOptions.RemoveEmptyEntries).Length,
        c.SourceType,
        c.SendSpeedMinSeconds,
        c.SendSpeedMaxSeconds);
}
