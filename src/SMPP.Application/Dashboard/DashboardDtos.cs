namespace SMPP.Application.Dashboard;

public record DashboardSummaryDto(
    decimal Balance,
    int SendsToday,
    double? DeliveryRatePercent,
    int PendingPayments);
