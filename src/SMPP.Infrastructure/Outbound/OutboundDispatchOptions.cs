namespace SMPP.Infrastructure.Outbound;

/// <summary>Bound from configuration key "OutboundDispatch".</summary>
public class OutboundDispatchOptions
{
    public const string SectionName = "OutboundDispatch";

    public int PollIntervalSeconds { get; set; } = 5;
    public int BatchSize { get; set; } = 25;
    public int DelayBetweenSendsMilliseconds { get; set; } = 500;
    public int MaxAttempts { get; set; } = 3;
}
