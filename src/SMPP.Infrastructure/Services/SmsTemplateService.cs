using Microsoft.EntityFrameworkCore;
using SMPP.Application.Common;
using SMPP.Application.SmsTemplates;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class SmsTemplateService : ISmsTemplateService
{
    private readonly SmppDbContext _db;

    public SmsTemplateService(SmppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<SmsTemplateListItemDto>> GetPagedAsync(int ownerUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.SmsTemplates.AsNoTracking().Where(t => t.CreatedByUserId == ownerUserId).OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<SmsTemplateListItemDto>
        {
            Items = items.Select(ToListItemDto).ToList(),
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task<SmsTemplateDetailDto?> GetByIdAsync(int id, int ownerUserId, CancellationToken ct = default)
    {
        var template = await FindOwnedAsync(id, ownerUserId, ct);
        return template is null ? null : ToDetailDto(template);
    }

    public async Task<int> CreateAsync(int ownerUserId, CreateSmsTemplateRequest request, CancellationToken ct = default)
    {
        var template = new SmsTemplate
        {
            Name = request.Name,
            Body = request.Body,
            CreatedByUserId = ownerUserId,
        };

        _db.SmsTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
        return template.Id;
    }

    public async Task UpdateAsync(int id, int ownerUserId, UpdateSmsTemplateRequest request, CancellationToken ct = default)
    {
        var template = await FindOwnedAsync(id, ownerUserId, ct)
            ?? throw new AppException("SMS template not found.");

        template.Name = request.Name;
        template.Body = request.Body;
        template.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, int ownerUserId, CancellationToken ct = default)
    {
        var template = await FindOwnedAsync(id, ownerUserId, ct)
            ?? throw new AppException("SMS template not found.");

        _db.SmsTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);
    }

    private Task<SmsTemplate?> FindOwnedAsync(int id, int ownerUserId, CancellationToken ct) =>
        _db.SmsTemplates.FirstOrDefaultAsync(t => t.Id == id && t.CreatedByUserId == ownerUserId, ct);

    private static SmsTemplateListItemDto ToListItemDto(SmsTemplate t) => new(
        t.Id, t.Name, t.Body, TemplatePlaceholders.Extract(t.Body), t.CreatedAt);

    private static SmsTemplateDetailDto ToDetailDto(SmsTemplate t) => new(
        t.Id, t.Name, t.Body, TemplatePlaceholders.Extract(t.Body), t.CreatedAt, t.UpdatedAt);
}
