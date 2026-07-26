using Microsoft.EntityFrameworkCore;
using SMPP.Application.Abstractions;
using SMPP.Application.Common;
using SMPP.Application.Templates;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Persistence;

namespace SMPP.Infrastructure.Services;

public class TemplateService : ITemplateService
{
    private readonly SmppDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly ISegmentCounter _segmentCounter;

    public TemplateService(SmppDbContext db, IFileStorageService fileStorage, ISegmentCounter segmentCounter)
    {
        _db = db;
        _fileStorage = fileStorage;
        _segmentCounter = segmentCounter;
    }

    public async Task<PagedResult<TemplateListItemDto>> GetPagedAsync(int ownerUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Templates.Where(t => t.CreatedByUserId == ownerUserId).OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TemplateListItemDto(t.Id, t.Name, t.TemplateCode, t.MessageSegmentCount, t.CsvFilePath, t.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<TemplateListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
        };
    }

    public async Task<TemplateDetailDto?> GetByIdAsync(int id, int ownerUserId, CancellationToken ct = default)
    {
        var template = await FindOwnedAsync(id, ownerUserId, ct);
        return template is null
            ? null
            : new TemplateDetailDto(template.Id, template.Name, template.TemplateCode, template.MessageBody, template.MessageSegmentCount, template.CsvFilePath);
    }

    public async Task<int> CreateAsync(int ownerUserId, CreateTemplateRequest request, CancellationToken ct = default)
    {
        var codeInUse = await _db.Templates.AnyAsync(t => t.TemplateCode == request.TemplateCode, ct);
        if (codeInUse)
        {
            throw new AppException($"Template code '{request.TemplateCode}' is already in use.");
        }

        var csvPath = await _fileStorage.SaveAsync(request.CsvFile, request.CsvFileName, "templates", ct);

        var template = new Template
        {
            Name = request.Name,
            TemplateCode = request.TemplateCode,
            MessageBody = request.MessageBody,
            MessageSegmentCount = _segmentCounter.CountSegments(request.MessageBody),
            CsvFilePath = csvPath,
            CreatedByUserId = ownerUserId,
        };

        _db.Templates.Add(template);
        await _db.SaveChangesAsync(ct);
        return template.Id;
    }

    public async Task UpdateAsync(int id, int ownerUserId, UpdateTemplateRequest request, CancellationToken ct = default)
    {
        var template = await FindOwnedAsync(id, ownerUserId, ct)
            ?? throw new AppException("Template not found.");

        template.Name = request.Name;
        template.MessageBody = request.MessageBody;
        template.MessageSegmentCount = _segmentCounter.CountSegments(request.MessageBody);
        template.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, int ownerUserId, CancellationToken ct = default)
    {
        var template = await FindOwnedAsync(id, ownerUserId, ct)
            ?? throw new AppException("Template not found.");

        _db.Templates.Remove(template);
        await _db.SaveChangesAsync(ct);
    }

    private Task<Template?> FindOwnedAsync(int id, int ownerUserId, CancellationToken ct) =>
        _db.Templates.FirstOrDefaultAsync(t => t.Id == id && t.CreatedByUserId == ownerUserId, ct);
}
