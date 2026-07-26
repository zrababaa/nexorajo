using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Identity;

namespace SMPP.Infrastructure.Persistence.Configurations;

public class OutboundMessageConfiguration : IEntityTypeConfiguration<OutboundMessage>
{
    public void Configure(EntityTypeBuilder<OutboundMessage> builder)
    {
        builder.Property(o => o.CampaignBatchId).HasMaxLength(50).IsRequired();
        builder.Property(o => o.SenderNumber).HasMaxLength(30).IsRequired();
        builder.Property(o => o.ReceiverNumber).HasMaxLength(30).IsRequired();
        builder.Property(o => o.MessageText).HasColumnType("text").IsRequired();
        builder.Property(o => o.LastError).HasColumnType("text");

        // OutboundMessageWorker polls Pending rows by Status - this index keeps that cheap.
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.CampaignBatchId);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(o => o.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
