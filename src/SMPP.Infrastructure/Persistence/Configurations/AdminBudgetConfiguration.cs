using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMPP.Domain.Entities;

namespace SMPP.Infrastructure.Persistence.Configurations;

public class AdminBudgetConfiguration : IEntityTypeConfiguration<AdminBudget>
{
    public void Configure(EntityTypeBuilder<AdminBudget> builder)
    {
        builder.Property(b => b.Balance).HasPrecision(18, 4);
    }
}
