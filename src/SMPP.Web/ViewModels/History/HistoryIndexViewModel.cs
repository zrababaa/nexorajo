using SMPP.Application.Common;
using SMPP.Application.History;
using SMPP.Domain.Enums;

namespace SMPP.Web.ViewModels.History;

public class HistoryIndexViewModel
{
    public required PagedResult<HistoryListItemDto> Page { get; init; }
    public required HistorySummaryDto Summary { get; init; }

    public MessageSource? Source { get; init; }
    public MessageStatus? Status { get; init; }
    public string? CampaignBatchId { get; init; }
    public string? Receiver { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
}
