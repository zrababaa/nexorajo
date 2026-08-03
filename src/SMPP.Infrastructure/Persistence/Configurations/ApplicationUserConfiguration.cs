using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMPP.Infrastructure.Identity;

namespace SMPP.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.Balance).HasPrecision(18, 4);
        builder.Property(u => u.RatePerMessage).HasPrecision(18, 4);
        builder.Property(u => u.SenderIds).HasMaxLength(500);
        builder.Property(u => u.ApiToken).HasMaxLength(128);
        builder.Property(u => u.ApiSecret).HasMaxLength(128);

        // Superadmin -> Account (2-tier, no reseller layer).
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(u => u.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // MySQL treats multiple NULLs in a UNIQUE index as distinct, so this is safe
        // for users who haven't had an API token generated yet (no HasFilter - not
        // supported by the MySQL provider).
        builder.HasIndex(u => u.ApiToken).IsUnique();
    }
}
