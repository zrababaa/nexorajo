using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Identity;

namespace SMPP.Infrastructure.Persistence.Configurations;

public class SpamKeywordConfiguration : IEntityTypeConfiguration<SpamKeyword>
{
    public void Configure(EntityTypeBuilder<SpamKeyword> builder)
    {
        builder.Property(k => k.Keyword).HasMaxLength(255).IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(k => k.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
