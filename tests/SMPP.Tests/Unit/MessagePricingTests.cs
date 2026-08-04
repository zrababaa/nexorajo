using SMPP.Domain.Pricing;
using Xunit;

namespace SMPP.Tests.Unit;

public class MessagePricingTests
{
    [Fact]
    public void One_recipient_one_part_costs_exactly_one_credit()
    {
        Assert.Equal(1m, MessagePricing.CostOf(recipientCount: 1, segments: 1));
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(1, 3, 3)]
    [InlineData(10, 1, 10)]
    [InlineData(10, 3, 30)]
    public void Cost_is_recipients_times_parts(int recipients, int segments, decimal expected)
    {
        Assert.Equal(expected, MessagePricing.CostOf(recipients, segments));
    }

    [Fact]
    public void Cost_is_never_fractional()
    {
        var cost = MessagePricing.CostOf(recipientCount: 7, segments: 2);
        Assert.Equal(decimal.Truncate(cost), cost);
    }
}
