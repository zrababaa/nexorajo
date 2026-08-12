using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using SMPP.Application.Abstractions;
using SMPP.Application.Accounts;
using SMPP.Application.AdminBudget;
using SMPP.Application.Campaigns;
using SMPP.Application.CompanyProfiles;
using SMPP.Application.Customers;
using SMPP.Application.Dashboard;
using SMPP.Application.History;
using SMPP.Application.Payments;
using SMPP.Application.PublicApi;
using SMPP.Application.Reports;
using SMPP.Application.Sending;
using SMPP.Application.SendingWindow;
using SMPP.Application.SpamKeywords;
using SMPP.Infrastructure.Email;
using SMPP.Infrastructure.Files;
using SMPP.Infrastructure.Identity;
using SMPP.Infrastructure.Jobs;
using SMPP.Infrastructure.Persistence;
using SMPP.Infrastructure.Segmenting;
using SMPP.Infrastructure.Services;

namespace SMPP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Escape hatch for local testing with no MySQL server available: everything above EF runs
        // unchanged against an in-memory database. Set via environment variable
        // (Database__UseInMemory=true) rather than checked-in appsettings, since it resets all
        // data on every restart and isn't something a shared dev/production config should default to.
        if (configuration.GetValue("Database:UseInMemory", defaultValue: false))
        {
            services.AddDbContext<SmppDbContext>(options => options.UseInMemoryDatabase("SmppDevDb"));
        }
        else
        {
            var connectionString = configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

            services.AddDbContext<SmppDbContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        }

        services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<SmppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<DatabaseBootstrapper>();

        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.AddScoped<ISegmentCounter, SegmentCounter>();
        services.AddScoped<ISendPolicyService, SendPolicyService>();
        services.AddScoped<IBalanceLedgerService, BalanceLedgerService>();
        services.AddScoped<IAdminBudgetService, AdminBudgetService>();
        services.AddScoped<ISendingWindowService, SendingWindowService>();
        services.AddScoped<IUserScopeResolver, UserScopeResolver>();
        services.AddScoped<ICampaignNumberParser, CampaignNumberParser>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<ISpamKeywordFilterService, SpamKeywordFilterService>();

        services.AddScoped<SendCore>();
        services.AddScoped<QuickSendHistoryReader>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ISpamKeywordService, SpamKeywordService>();
        services.AddScoped<IQuickSendService, QuickSendService>();
        services.AddScoped<IBulkSendService, BulkSendService>();
        services.AddScoped<IHistoryService, HistoryService>();
        services.AddScoped<IHistoryResendService, HistoryResendService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<ICompanyProfileService, CompanyProfileService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IPublicApiAuthenticator, PublicApiAuthenticator>();
        services.AddScoped<IPublicApiSendService, PublicApiSendService>();

        // In-memory job store: ScheduledSends (not Quartz) is the durable source of truth, and
        // ScheduledSendRecoveryHostedService rebuilds the scheduler's triggers from it on every
        // startup. No jobs are registered here - ScheduledSendService/the recovery service add
        // them dynamically, one per scheduled send.
        services.AddQuartz();
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
        services.AddScoped<IScheduledSendService, ScheduledSendService>();
        services.AddHostedService<ScheduledSendRecoveryHostedService>();

        return services;
    }
}
