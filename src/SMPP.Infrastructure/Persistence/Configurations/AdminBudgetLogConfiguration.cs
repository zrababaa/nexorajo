using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Identity;

namespace SMPP.Infrastructure.Persistence.Configurations;

public class AdminBudgetLogConfiguration : IEntityTypeConfiguration<AdminBudgetLog>
{
    public void Configure(EntityTypeBuilder<AdminBudgetLog> builder)
    {
        builder.Property(l => l.Amount).HasPrecision(18, 4);
        builder.Property(l => l.BalanceAfter).HasPrecision(18, 4);
        builder.HasIndex(l => l.CreatedAt);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(l => l.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(l => l.RelatedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
