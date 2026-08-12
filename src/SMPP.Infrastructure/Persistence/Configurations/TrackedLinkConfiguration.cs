using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Identity;

namespace SMPP.Infrastructure.Persistence.Configurations;

public class TrackedLinkConfiguration : IEntityTypeConfiguration<TrackedLink>
{
    public void Configure(EntityTypeBuilder<TrackedLink> builder)
    {
        builder.Property(t => t.Token).HasMaxLength(10).IsRequired();
        builder.Property(t => t.BatchId).HasMaxLength(25).IsRequired();
        builder.Property(t => t.DestinationUrl).HasMaxLength(2048).IsRequired();

        builder.HasIndex(t => t.Token).IsUnique();
        builder.HasIndex(t => t.BatchId);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
