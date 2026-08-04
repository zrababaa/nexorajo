using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Identity;

namespace SMPP.Infrastructure.Persistence.Configurations;

public class SpamBlockedAttemptConfiguration : IEntityTypeConfiguration<SpamBlockedAttempt>
{
    public void Configure(EntityTypeBuilder<SpamBlockedAttempt> builder)
    {
        builder.Property(a => a.SenderId).HasMaxLength(30).IsRequired();
        builder.Property(a => a.Message).HasColumnType("text").IsRequired();
        builder.Property(a => a.MatchedTerms).HasMaxLength(500).IsRequired();

        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => a.UserId);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
