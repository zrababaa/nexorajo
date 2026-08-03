using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMPP.Domain.Entities;

namespace SMPP.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps onto the legacy <c>quick_send_history</c> table, where the external SMPP daemon logs
/// Quick Send recipients. Excluded from migrations and carrying no foreign key on
/// <c>creater_id</c>: the table belongs to the daemon and predates this app, so EF must never
/// try to create, alter, or constrain it - it is read-only from here.
/// </summary>
public class QuickSendHistoryConfiguration : IEntityTypeConfiguration<QuickSendHistory>
{
    public void Configure(EntityTypeBuilder<QuickSendHistory> builder)
    {
        builder.ToTable("quick_send_history", t => t.ExcludeFromMigrations());

        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.CampaignBatchId).HasColumnName("camp_id").HasMaxLength(25).IsRequired();
        builder.Property(h => h.CampaignName).HasColumnName("camp_name").HasMaxLength(255);
        builder.Property(h => h.SenderNumber).HasColumnName("sender_no").HasMaxLength(30).IsRequired();
        builder.Property(h => h.ReceiverNumber).HasColumnName("receiver_no").HasMaxLength(30).IsRequired();
        builder.Property(h => h.MessageText).HasColumnName("message").HasColumnType("text");
        builder.Property(h => h.Status).HasColumnName("status").HasMaxLength(10).HasConversion(LegacyMessageCodes.Status);
        builder.Property(h => h.ExternalMessageId).HasColumnName("get_message_id").HasMaxLength(100);
        builder.Property(h => h.GatewayResponse).HasColumnName("response").HasColumnType("text");
        builder.Property(h => h.CreatedByUserId).HasColumnName("creater_id");
        builder.Property(h => h.CreatedAt).HasColumnName("created_at");
        builder.Property(h => h.UpdatedAt).HasColumnName("updated_at");
    }
}
