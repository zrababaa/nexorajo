using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Identity;

namespace SMPP.Infrastructure.Persistence.Configurations;

public class SmsTemplateConfiguration : IEntityTypeConfiguration<SmsTemplate>
{
    public void Configure(EntityTypeBuilder<SmsTemplate> builder)
    {
        builder.Property(t => t.Name).HasMaxLength(150).IsRequired();
        builder.Property(t => t.Body).HasColumnType("longtext").IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
