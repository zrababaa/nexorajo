using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Identity;

namespace SMPP.Infrastructure.Persistence;

public class SmppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
{
    public SmppDbContext(DbContextOptions<SmppDbContext> options) : base(options)
    {
    }

    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<SpamKeyword> SpamKeywords => Set<SpamKeyword>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<History> Histories => Set<History>();
    public DbSet<QuickSendHistory> QuickSendHistories => Set<QuickSendHistory>();
    public DbSet<UnderProcess> UnderProcessBatches => Set<UnderProcess>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AdminBudget> AdminBudgets => Set<AdminBudget>();
    public DbSet<AdminBudgetLog> AdminBudgetLogs => Set<AdminBudgetLog>();
    public DbSet<SpamBlockedAttempt> SpamBlockedAttempts => Set<SpamBlockedAttempt>();
    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();
    public DbSet<CompanyDocument> CompanyDocuments => Set<CompanyDocument>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(SmppDbContext).Assembly);
    }
}
