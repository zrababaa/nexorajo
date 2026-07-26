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
        builder.Property(u => u.WhiteLabelDomain).HasMaxLength(255);
        builder.Property(u => u.ApiToken).HasMaxLength(128);
        builder.Property(u => u.ApiSecret).HasMaxLength(128);

        // Self-referential ownership chain: Superadmin -> WhiteLabelAdmin -> EndUser.
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(u => u.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Real FK, fixing legacy's package_id-stored-as-name-string bug.
        builder.HasOne<Domain.Entities.UserPackage>()
            .WithMany()
            .HasForeignKey(u => u.PackageId)
            .OnDelete(DeleteBehavior.SetNull);

        // MySQL treats multiple NULLs in a UNIQUE index as distinct, so this is safe
        // for users who haven't had an API token generated yet (no HasFilter - not
        // supported by the MySQL provider).
        builder.HasIndex(u => u.ApiToken).IsUnique();
    }
}
