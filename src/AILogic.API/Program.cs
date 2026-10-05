using System.Threading.RateLimiting;
using AILogic.API.Configuration;
using AILogic.API.Middleware;
using AILogic.Application.Services;
using AILogic.Infrastructure;
using AILogic.Infrastructure.Configuration;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var keyFileSettings = KeyFileLoader.LoadFromProjectTree(builder.Environment.ContentRootPath);
builder.Configuration.AddInMemoryCollection(keyFileSettings);
builder.Configuration.AddEnvironmentVariables();

var agentOptions = new DigitalOceanAgentOptions
{
    Endpoint = builder.Configuration["DIGITALOCEAN_AGENT_ENDPOINT"] ?? string.Empty,
    AccessKey = builder.Configuration["DIGITALOCEAN_AGENT_KEY"] ?? string.Empty
};

var metaOptions = new MetaOptions
{
    GraphApiVersion = builder.Configuration["META_GRAPH_API_VERSION"] ?? "v22.0",
    WebhookVerifyToken = builder.Configuration["META_WEBHOOK_VERIFY_TOKEN"] ?? string.Empty,
    MessengerPageAccessToken = builder.Configuration["META_MESSENGER_PAGE_ACCESS_TOKEN"] ?? string.Empty,
    WhatsAppAccessToken = builder.Configuration["META_WHATSAPP_ACCESS_TOKEN"] ?? string.Empty,
    WhatsAppPhoneNumberId = builder.Configuration["META_WHATSAPP_PHONE_NUMBER_ID"] ?? string.Empty
};

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IChatService, ChatService>();
builder.Services.AddSingleton<MessengerService>();
builder.Services.AddSingleton<WhatsAppService>();
builder.Services.AddInfrastructure(agentOptions, metaOptions);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("chat", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseRateLimiter();

if (Directory.Exists(app.Environment.WebRootPath))
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}
else if (KeyFileLoader.FindSiteRoot(app.Environment.ContentRootPath) is { } siteRoot)
{
    var siteFiles = new PhysicalFileProvider(siteRoot);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = siteFiles });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = siteFiles });
}

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    agentConfigured = !string.IsNullOrWhiteSpace(agentOptions.Endpoint) &&
                      !string.IsNullOrWhiteSpace(agentOptions.AccessKey),
    messengerConfigured = !string.IsNullOrWhiteSpace(metaOptions.MessengerPageAccessToken),
    whatsAppConfigured = !string.IsNullOrWhiteSpace(metaOptions.WhatsAppAccessToken) &&
                         !string.IsNullOrWhiteSpace(metaOptions.WhatsAppPhoneNumberId)
}));

app.Run();

public partial class Program;
