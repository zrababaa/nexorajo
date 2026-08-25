using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SMPP.Application.Common;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Persistence;
using SMPP.Infrastructure.Services;
using Xunit;

namespace SMPP.Tests.Unit;

public class TemplateMessageResolverTests
{
    private static SmppDbContext BuildDb()
    {
        var options = new DbContextOptionsBuilder<SmppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SmppDbContext(options);
    }

    [Fact]
    public async Task RenderAsync_fills_imported_columns_per_recipient()
    {
        using var db = BuildDb();
        var recipientVariablesJson = JsonSerializer.Serialize(new Dictionary<string, Dictionary<string, string>>
        {
            ["97150111"] = new() { ["Name"] = "Ali", ["Amount"] = "100" },
            ["97150222"] = new() { ["Name"] = "Sara", ["Amount"] = "200" },
        });
        var importedColumnsJson = JsonSerializer.Serialize(new[] { "Name", "Amount" });

        var result = await TemplateMessageResolver.RenderAsync(
            db, ownerUserId: 1, numbers: ["97150111", "97150222"],
            templateBody: "Hi [Name], you owe [Amount]",
            variables: null,
            recipientVariablesJson: recipientVariablesJson,
            importedColumnsJson: importedColumnsJson,
            ct: default);

        Assert.Equal("Hi Ali, you owe 100", result["97150111"]);
        Assert.Equal("Hi Sara, you owe 200", result["97150222"]);
    }

    [Fact]
    public async Task RenderAsync_imported_column_overrides_same_named_customer_field()
    {
        using var db = BuildDb();
        db.Customers.Add(new Customer { AccountId = 1, Name = "Customer Name", Phone = "97150111" });
        await db.SaveChangesAsync();

        var recipientVariablesJson = JsonSerializer.Serialize(new Dictionary<string, Dictionary<string, string>>
        {
            ["97150111"] = new() { ["Name"] = "Row Name" },
        });
        var importedColumnsJson = JsonSerializer.Serialize(new[] { "Name" });

        var result = await TemplateMessageResolver.RenderAsync(
            db, ownerUserId: 1, numbers: ["97150111"],
            templateBody: "Hi [Name]",
            variables: null,
            recipientVariablesJson: recipientVariablesJson,
            importedColumnsJson: importedColumnsJson,
            ct: default);

        Assert.Equal("Hi Row Name", result["97150111"]);
    }

    [Fact]
    public async Task RenderAsync_throws_when_a_still_global_placeholder_has_no_value()
    {
        using var db = BuildDb();

        var ex = await Assert.ThrowsAsync<AppException>(() => TemplateMessageResolver.RenderAsync(
            db, ownerUserId: 1, numbers: ["97150111"],
            templateBody: "Hi [Name], code [PromoCode]",
            variables: null,
            recipientVariablesJson: null,
            importedColumnsJson: null,
            ct: default));

        Assert.Contains("PromoCode", ex.Message);
    }
}
