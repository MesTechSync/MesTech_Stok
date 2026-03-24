using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class ProfitReportConfiguration : IEntityTypeConfiguration<ProfitReport>
{
    public void Configure(EntityTypeBuilder<ProfitReport> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.ReportDate })
            .HasDatabaseName("IX_ProfitReports_Tenant_Date");

        builder.HasIndex(e => new { e.TenantId, e.Platform })
            .HasFilter("\"Platform\" IS NOT NULL")
            .HasDatabaseName("IX_ProfitReports_Tenant_Platform");

        builder.Property(e => e.Platform).HasMaxLength(50);
        builder.Property(e => e.Period).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TotalRevenue).HasPrecision(18, 2);
        builder.Property(e => e.TotalCost).HasPrecision(18, 2);
        builder.Property(e => e.TotalCommission).HasPrecision(18, 2);
        builder.Property(e => e.TotalCargo).HasPrecision(18, 2);
        builder.Property(e => e.TotalTax).HasPrecision(18, 2);
        builder.Property(e => e.NetProfit).HasPrecision(18, 2);
    }
}
