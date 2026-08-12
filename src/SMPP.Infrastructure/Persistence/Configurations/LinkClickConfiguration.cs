using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMPP.Domain.Entities;

namespace SMPP.Infrastructure.Persistence.Configurations;

public class LinkClickConfiguration : IEntityTypeConfiguration<LinkClick>
{
    public void Configure(EntityTypeBuilder<LinkClick> builder)
    {
        builder.Property(c => c.BatchId).HasMaxLength(25).IsRequired();
        builder.Property(c => c.IpAddress).HasMaxLength(45).IsRequired();
        builder.Property(c => c.UserAgent).HasMaxLength(512);

        builder.HasIndex(c => new { c.BatchId, c.ClickedAt });
        builder.HasIndex(c => c.TrackedLinkId);

        builder.HasOne<TrackedLink>()
            .WithMany()
            .HasForeignKey(c => c.TrackedLinkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
