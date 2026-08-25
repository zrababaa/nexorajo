using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Identity;
using SMPP.Infrastructure.Persistence;
using SMPP.Infrastructure.Services;
using Xunit;

namespace SMPP.Tests.Integration;

/// <summary>
/// End-to-end proof that a headered CSV import actually persists its columns through the real
/// HTTP/DB pipeline (the part that was previously wired up in memory only, in
/// <c>CampaignNumberParser</c>, but never saved - see <c>CampaignsApiController.CreateAsync</c>/
/// <c>CampaignService.CreateAsync</c>), and that <c>TemplateMessageResolver</c> renders those
/// columns from the persisted <see cref="SMPP.Domain.Entities.Campaign"/> row, not just from a
/// hand-built JSON string as in <see cref="TemplateMessageResolverTests"/>.
/// </summary>
public class CampaignImportedVariablesIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CampaignImportedVariablesIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Captured once, outside the ConfigureServices callback: WebApplicationFactory can invoke
        // that callback more than once while building/rebuilding the host, and a fresh Guid on
        // each invocation would silently split the test across two different empty databases.
        var databaseName = Guid.NewGuid().ToString();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Database:UseInMemory", "true");
            builder.ConfigureServices(services =>
            {
                // Give this test its own isolated in-memory database instead of the fixed
                // "SmppDevDb" name AddInfrastructure uses for local dev, which is a shared static
                // store that would otherwise leak state across test runs.
                services.RemoveAll<DbContextOptions<SmppDbContext>>();
                services.AddDbContext<SmppDbContext>(o => o.UseInMemoryDatabase(databaseName));
            });
        });
    }

    [Fact]
    public async Task Imported_columns_survive_the_full_import_pipeline_and_render_per_recipient()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Superadmin sends for free (SendCore.isFree), so this test doesn't also need to seed a
        // balance/sender-id policy just to prove the import+template-resolution wiring.
        var user = new ApplicationUser
        {
            UserName = "resolver-test@example.com",
            Email = "resolver-test@example.com",
            FullName = "Resolver Test",
            Role = UserRole.Superadmin,
            IsActive = true,
        };
        var createResult = await userManager.CreateAsync(user, "Test1234!");
        Assert.True(createResult.Succeeded, string.Join(", ", createResult.Errors.Select(e => e.Description)));

        using var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { identifier = user.Email, password = "Test1234!" });
        Assert.True(loginResponse.IsSuccessStatusCode, await loginResponse.Content.ReadAsStringAsync());
        var login = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = login.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var importForm = new MultipartFormDataContent
        {
            { new StringContent("Excel Import Test"), "name" },
        };
        var csv = Encoding.UTF8.GetBytes("Phone,Name,Amount\n97150111,Ali,100\n97150222,Sara,200\n");
        var fileContent = new ByteArrayContent(csv);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
        importForm.Add(fileContent, "file", "recipients.csv");

        var importResponse = await client.PostAsync("/api/v1/campaigns/import", importForm);
        Assert.True(importResponse.IsSuccessStatusCode, await importResponse.Content.ReadAsStringAsync());
        var campaignJson = await importResponse.Content.ReadFromJsonAsync<JsonElement>();

        var importedColumns = campaignJson.GetProperty("importedColumns").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Equal(new[] { "Phone", "Name", "Amount" }, importedColumns);
        var campaignId = campaignJson.GetProperty("id").GetInt32();

        var templateResponse = await client.PostAsJsonAsync(
            "/api/v1/sms-templates", new { name = "Excel vars template", body = "Hi [Name], you owe [Amount]" });
        Assert.True(templateResponse.IsSuccessStatusCode, await templateResponse.Content.ReadAsStringAsync());
        var templateJson = await templateResponse.Content.ReadFromJsonAsync<JsonElement>();
        var templateId = templateJson.GetProperty("id").GetInt32();

        // Re-fetch the campaign the same way the send pipeline does, to prove the columns/values
        // round-tripped through the database rather than only existing in the HTTP response.
        using var db = scope.ServiceProvider.GetRequiredService<SmppDbContext>();
        var campaign = await db.Campaigns.FirstAsync(c => c.Id == campaignId);

        var rendered = await TemplateMessageResolver.ResolveAsync(
            db, user.Id, campaign, message: null, templateId, variables: null, CancellationToken.None);

        Assert.Equal("Hi Ali, you owe 100", rendered["97150111"]);
        Assert.Equal("Hi Sara, you owe 200", rendered["97150222"]);
    }
}
