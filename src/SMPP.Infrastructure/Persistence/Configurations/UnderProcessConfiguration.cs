using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMPP.Domain.Entities;

namespace SMPP.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps onto the legacy <c>under_process</c> table verbatim. The external SMPP daemon reads
/// this table with hard-coded column names, so every mapping here is load-bearing - renaming
/// anything silently breaks sending. No foreign key on userId either: the daemon deletes or
/// archives rows on its own schedule and a cascade/restrict rule would fight it.
/// </summary>
public class UnderProcessConfiguration : IEntityTypeConfiguration<UnderProcess>
{
    public void Configure(EntityTypeBuilder<UnderProcess> builder)
    {
        builder.ToTable("under_process");

        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.CampaignId).HasColumnName("camp_id").HasMaxLength(25).IsRequired();
        builder.Property(u => u.SenderId).HasColumnName("senderId").HasMaxLength(11).IsRequired();
        builder.Property(u => u.Message).HasColumnName("message").HasColumnType("text").IsRequired();
        builder.Property(u => u.CampaignNumbers).HasColumnName("camp_numbers").HasColumnType("longtext").IsRequired();
        builder.Property(u => u.CampaignName).HasColumnName("camp_name").HasMaxLength(255);
        builder.Property(u => u.PushType).HasColumnName("push_type").HasConversion<int>();
        builder.Property(u => u.UserId).HasColumnName("userId");
        builder.Property(u => u.Priority).HasColumnName("priority").HasConversion<int>();
        builder.Property(u => u.FilePath).HasColumnName("file_path").HasMaxLength(255);

        // Left to the database, because legacy inserts here with the query builder
        // (DB::table('under_process')->insert([...])) and never supplies a timestamp - so both
        // columns carry MySQL defaults and are NOT NULL. EF sending an explicit NULL for
        // updated_at overrides the default and the insert fails outright.
        //
        // It also keeps the clocks consistent. Legacy runs on Asia/Amman, so every row in this
        // database is local time; DateTime.UtcNow from this app would be three hours behind the
        // daemon's own rows. Letting MySQL stamp them means one clock for everyone, whatever the
        // app server's timezone happens to be.
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").ValueGeneratedOnAdd();
        builder.Property(u => u.CreatedAt).Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

        builder.Property(u => u.UpdatedAt).HasColumnName("updated_at").ValueGeneratedOnAddOrUpdate();
        builder.Property(u => u.UpdatedAt).Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);

        builder.HasIndex(u => u.CampaignId).IsUnique();
    }
}
