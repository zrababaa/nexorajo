namespace SMPP.Application.Templates;

public record TemplateListItemDto(
    int Id,
    string Name,
    string TemplateCode,
    int MessageSegmentCount,
    string? CsvFilePath,
    DateTime CreatedAt);

public record TemplateDetailDto(
    int Id,
    string Name,
    string TemplateCode,
    string MessageBody,
    int MessageSegmentCount,
    string? CsvFilePath);

public record CreateTemplateRequest(
    string Name,
    string TemplateCode,
    string MessageBody,
    Stream CsvFile,
    string CsvFileName);

public record UpdateTemplateRequest(
    string Name,
    string MessageBody);
