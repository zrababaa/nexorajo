using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMPP.Domain.Entities;
using SMPP.Infrastructure.Identity;

namespace SMPP.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.Amount).HasPrecision(18, 4);
        builder.Property(p => p.TransactionRef).HasMaxLength(150);
        builder.Property(p => p.ProofFilePath).HasMaxLength(500);
        builder.Property(p => p.Note).HasColumnType("text");
        builder.Property(p => p.ReviewNote).HasColumnType("text");

        builder.HasIndex(p => p.SubmittedByUserId);
        builder.HasIndex(p => p.Status);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(p => p.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
