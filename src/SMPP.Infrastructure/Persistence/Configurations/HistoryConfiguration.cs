using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Identity;

namespace SMPP.Infrastructure.Persistence.Configurations;

public class HistoryConfiguration : IEntityTypeConfiguration<History>
{
    public void Configure(EntityTypeBuilder<History> builder)
    {
        builder.Property(h => h.CampaignBatchId).HasMaxLength(50).IsRequired();
        builder.Property(h => h.SenderNumber).HasMaxLength(30).IsRequired();
        builder.Property(h => h.ReceiverNumber).HasMaxLength(30).IsRequired();
        builder.Property(h => h.MessageText).HasColumnType("text");
        builder.Property(h => h.ExternalMessageId).HasMaxLength(100);
        builder.Property(h => h.GatewayResponse).HasColumnType("text");

        builder.HasIndex(h => h.CampaignBatchId);
        builder.HasIndex(h => new { h.CreatedByUserId, h.CreatedAt });
        builder.HasIndex(h => h.ExternalMessageId);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(h => h.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
