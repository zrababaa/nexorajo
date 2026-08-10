using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Identity;

namespace SMPP.Infrastructure.Persistence.Configurations;

public class CompanyDocumentConfiguration : IEntityTypeConfiguration<CompanyDocument>
{
    public void Configure(EntityTypeBuilder<CompanyDocument> builder)
    {
        builder.Property(d => d.FileName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.FilePath).HasMaxLength(500).IsRequired();

        builder.HasIndex(d => d.CompanyProfileId);
        builder.HasIndex(d => d.AccountId);

        builder.HasOne<CompanyProfile>()
            .WithMany()
            .HasForeignKey(d => d.CompanyProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(d => d.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
