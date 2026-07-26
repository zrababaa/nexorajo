using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SMPP.Application.Abstractions;
using SMPP.Application.Accounts;
using SMPP.Application.Campaigns;
using SMPP.Application.Dashboard;
using SMPP.Application.History;
using SMPP.Application.PublicApi;
using SMPP.Application.Sending;
using SMPP.Application.SpamKeywords;
using SMPP.Infrastructure.Files;
using SMPP.Infrastructure.Identity;
using SMPP.Infrastructure.Outbound;
using SMPP.Infrastructure.Persistence;
using SMPP.Infrastructure.Segmenting;
using SMPP.Infrastructure.Services;
using SMPP.Infrastructure.SmsGateway;

namespace SMPP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<SmppDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<SmppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<GatewayOptions>(configuration.GetSection(GatewayOptions.SectionName));
        services.Configure<OutboundDispatchOptions>(configuration.GetSection(OutboundDispatchOptions.SectionName));

        services.AddHttpClient<ISmsGatewayClient, SmsGatewayClient>();

        services.AddScoped<ISegmentCounter, SegmentCounter>();
        services.AddScoped<IBalanceLedgerService, BalanceLedgerService>();
        services.AddScoped<IUserScopeResolver, UserScopeResolver>();
        services.AddScoped<ICampaignNumberParser, CampaignNumberParser>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<ISpamKeywordFilterService, SpamKeywordFilterService>();

        services.AddScoped<SendCore>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ISpamKeywordService, SpamKeywordService>();
        services.AddScoped<IQuickSendService, QuickSendService>();
        services.AddScoped<IBulkSendService, BulkSendService>();
        services.AddScoped<IHistoryService, HistoryService>();
        services.AddScoped<IPublicApiAuthenticator, PublicApiAuthenticator>();
        services.AddScoped<IPublicApiSendService, PublicApiSendService>();

        services.AddHostedService<OutboundMessageWorker>();

        return services;
    }
}
