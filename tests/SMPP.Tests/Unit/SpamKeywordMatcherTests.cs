using SMPP.Infrastructure.Services;
using Xunit;

namespace SMPP.Tests.Unit;

public class SpamKeywordMatcherTests
{
    [Theory]
    [InlineData("Claim your free cash today", "cash", true)]
    [InlineData("Claim your free CASH today", "cash", true)]
    [InlineData("Have a cashew", "cash", false)]
    [InlineData("Nothing to see here", "cash", false)]
    [InlineData("50%off this week", "%off", true)]
    public void Keywords_match_on_word_boundaries_case_insensitively(string message, string keyword, bool expected)
    {
        Assert.Equal(expected, SpamKeywordMatcher.ContainsKeyword(message, keyword));
    }

    [Theory]
    [InlineData("اربح جائزة الآن", "جائزة", true)]
    [InlineData("اربح جوائز الآن", "جائزة", false)]
    public void Arabic_keywords_match_on_word_boundaries(string message, string keyword, bool expected)
    {
        Assert.Equal(expected, SpamKeywordMatcher.ContainsKeyword(message, keyword));
    }

    [Fact]
    public void Blank_keyword_never_matches()
    {
        Assert.False(SpamKeywordMatcher.ContainsKeyword("anything at all", "   "));
    }

    [Theory]
    [InlineData("Open https://bit.ly/x9f now", "bit.ly", true)]
    [InlineData("Open https://BIT.LY/x9f now", "bit.ly", true)]
    [InlineData("Open https://example.com now", "bit.ly", false)]
    public void Urls_match_as_substrings(string message, string url, bool expected)
    {
        Assert.Equal(expected, SpamKeywordMatcher.ContainsUrl(message, url));
    }
}
