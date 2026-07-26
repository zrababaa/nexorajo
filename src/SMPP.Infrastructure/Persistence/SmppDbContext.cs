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

    public DbSet<UserPackage> UserPackages => Set<UserPackage>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Instance> Instances => Set<Instance>();
    public DbSet<Proxy> Proxies => Set<Proxy>();
    public DbSet<SpamKeyword> SpamKeywords => Set<SpamKeyword>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<History> Histories => Set<History>();
    public DbSet<OutboundMessage> OutboundMessages => Set<OutboundMessage>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(SmppDbContext).Assembly);
    }
}
