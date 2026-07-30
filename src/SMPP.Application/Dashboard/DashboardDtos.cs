using SMPP.Domain.Enums;

namespace SMPP.Application.Dashboard;

public record DashboardSummaryDto(
    decimal Balance,
    int SendsToday,
    double? DeliveryRatePercent,
    int PendingPayments,
    int? TotalAccounts,
    decimal? TotalCreditsIssued);

/// <summary>One day's send volume for the trend chart, always UTC-dated.</summary>
public record DashboardTrendPointDto(
    DateOnly Date,
    int Total,
    int Delivered);

public record DashboardStatusSliceDto(
    MessageStatus Status,
    int Count);

public record DashboardSourceSliceDto(
    MessageSource Source,
    int Count);
