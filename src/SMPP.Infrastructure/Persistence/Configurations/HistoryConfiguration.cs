using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SMPP.Domain.Entities;
using SMPP.Domain.Enums;
using SMPP.Infrastructure.Identity;

namespace SMPP.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps onto the legacy <c>historys</c> table verbatim, because the external SMPP daemon
/// inserts these rows and its DLR callback updates them by <c>get_message_id</c>. Status and
/// message_type stay as legacy text codes rather than enum ordinals for the same reason. Both
/// converters fall back to a default on an unrecognised code, so a daemon-side vocabulary
/// change degrades to a mislabelled row instead of a query that throws.
/// </summary>
public class HistoryConfiguration : IEntityTypeConfiguration<History>
{
    private static readonly ValueConverter<MessageStatus, string> StatusConverter = new(
        status => status == MessageStatus.Delivered ? "DELIVRD"
            : status == MessageStatus.Sent ? "SENT"
            : status == MessageStatus.Undelivered ? "UNDELIV"
            : status == MessageStatus.Expired ? "EXPIRED"
            : status == MessageStatus.Failed ? "FAILED"
            : "PROCESS",
        code => code == "DELIVRD" ? MessageStatus.Delivered
            : code == "SENT" ? MessageStatus.Sent
            : code == "UNDELIV" ? MessageStatus.Undelivered
            : code == "EXPIRED" ? MessageStatus.Expired
            : code == "FAILED" ? MessageStatus.Failed
            : MessageStatus.Processing);

    private static readonly ValueConverter<MessageSource, string> SourceConverter = new(
        source => source == MessageSource.BulkSend ? "BTXTM"
            : source == MessageSource.PublicApi ? "ATXTM"
            : "STXTM",
        code => code == "BTXTM" ? MessageSource.BulkSend
            : code == "ATXTM" ? MessageSource.PublicApi
            : MessageSource.QuickSend);

    public void Configure(EntityTypeBuilder<History> builder)
    {
        builder.ToTable("historys");

        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.CampaignBatchId).HasColumnName("camp_id").HasMaxLength(25).IsRequired();
        builder.Property(h => h.CampaignName).HasColumnName("camp_name").HasMaxLength(255);
        builder.Property(h => h.Source).HasColumnName("message_type").HasMaxLength(10).HasConversion(SourceConverter);
        builder.Property(h => h.SenderNumber).HasColumnName("sender_no").HasMaxLength(30).IsRequired();
        builder.Property(h => h.ReceiverNumber).HasColumnName("receiver_no").HasMaxLength(30).IsRequired();
        builder.Property(h => h.MessageText).HasColumnName("message").HasColumnType("text");
        builder.Property(h => h.Status).HasColumnName("status").HasMaxLength(10).HasConversion(StatusConverter);
        builder.Property(h => h.ExternalMessageId).HasColumnName("get_message_id").HasMaxLength(100);
        builder.Property(h => h.GatewayResponse).HasColumnName("response").HasColumnType("text");
        builder.Property(h => h.CreatedByUserId).HasColumnName("creater_id");
        builder.Property(h => h.CreatedAt).HasColumnName("created_at");
        builder.Property(h => h.UpdatedAt).HasColumnName("updated_at");

        builder.HasIndex(h => h.CampaignBatchId);
        builder.HasIndex(h => new { h.CreatedByUserId, h.CreatedAt });
        builder.HasIndex(h => h.ExternalMessageId);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(h => h.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
