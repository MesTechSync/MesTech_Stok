using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class AccountingPeriodConfiguration : IEntityTypeConfiguration<AccountingPeriod>
{
    public void Configure(EntityTypeBuilder<AccountingPeriod> builder)
    {
        builder.HasIndex(p => new { p.TenantId, p.Year, p.Month })
            .IsUnique()
            .HasDatabaseName("IX_AccountingPeriods_Tenant_Year_Month");

        builder.Property(p => p.ClosedByUserId).HasMaxLength(100);
    }
}
