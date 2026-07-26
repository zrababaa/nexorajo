using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SMPP.Application.Abstractions;
using SMPP.Application.Campaigns;
using SMPP.Application.Dashboard;
using SMPP.Application.Templates;
using SMPP.Infrastructure.Files;
using SMPP.Infrastructure.Identity;
using SMPP.Infrastructure.Outbound;
using SMPP.Infrastructure.Persistence;
using SMPP.Infrastructure.Segmenting;
using SMPP.Infrastructure.Services;
using SMPP.Infrastructure.WhatsAppGateway;

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

        services.AddHttpClient<IWhatsAppGatewayClient, WhatsAppGatewayClient>();

        services.AddScoped<ISegmentCounter, SegmentCounter>();
        services.AddScoped<IBalanceLedgerService, BalanceLedgerService>();
        services.AddScoped<IUserScopeResolver, UserScopeResolver>();
        services.AddScoped<ICampaignNumberParser, CampaignNumberParser>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<IDashboardService, DashboardService>();

        services.AddHostedService<OutboundMessageWorker>();

        return services;
    }
}
