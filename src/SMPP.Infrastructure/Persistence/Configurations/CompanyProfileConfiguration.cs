using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Identity;

namespace SMPP.Infrastructure.Persistence.Configurations;

public class CompanyProfileConfiguration : IEntityTypeConfiguration<CompanyProfile>
{
    public void Configure(EntityTypeBuilder<CompanyProfile> builder)
    {
        builder.Property(p => p.RegistrationId).HasMaxLength(100);
        builder.Property(p => p.CompanyName).HasMaxLength(200);
        builder.Property(p => p.Address).HasMaxLength(300);
        builder.Property(p => p.Phone).HasMaxLength(50);
        builder.Property(p => p.Email).HasMaxLength(200);
        builder.Property(p => p.Website).HasMaxLength(200);
        builder.Property(p => p.Description).HasColumnType("text");
        builder.Property(p => p.LogoPath).HasMaxLength(500);

        builder.HasIndex(p => p.AccountId).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
