using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Identity;

namespace SMPP.Infrastructure.Persistence.Configurations;

public class InstanceConfiguration : IEntityTypeConfiguration<Instance>
{
    public void Configure(EntityTypeBuilder<Instance> builder)
    {
        builder.Property(i => i.ExternalInstanceId).HasMaxLength(100).IsRequired();
        builder.Property(i => i.WhatsAppNumber).HasMaxLength(30).IsRequired();
        builder.Property(i => i.Name).HasMaxLength(150).IsRequired();
        builder.Property(i => i.CallbackUrl).HasMaxLength(500);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(i => i.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
