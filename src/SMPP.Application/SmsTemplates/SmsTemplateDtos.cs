namespace SMPP.Application.SmsTemplates;

public record SmsTemplateListItemDto(
    int Id,
    string Name,
    string Body,
    IReadOnlyList<string> Placeholders,
    DateTime CreatedAt);

public record SmsTemplateDetailDto(
    int Id,
    string Name,
    string Body,
    IReadOnlyList<string> Placeholders,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateSmsTemplateRequest(string Name, string Body);

public record UpdateSmsTemplateRequest(string Name, string Body);
