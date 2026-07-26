using SMPP.Domain.Common;

namespace SMPP.Domain.Entities;

public class Template : AuditableEntity, IHasCreator
{
    public string Name { get; set; } = string.Empty;
    public string MessageBody { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string? CsvFilePath { get; set; }
    public int MessageSegmentCount { get; set; }
    public int CreatedByUserId { get; set; }
}
