using MesTech.Domain.Entities.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class SavedReportConfiguration : IEntityTypeConfiguration<SavedReport>
{
    public void Configure(EntityTypeBuilder<SavedReport> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.ReportType).HasMaxLength(50);
        builder.Property(e => e.FilterJson).HasMaxLength(4000);

        builder.HasIndex(e => new { e.TenantId, e.CreatedByUserId })
            .HasDatabaseName("IX_SavedReports_Tenant_Creator");

        builder.HasIndex(e => new { e.TenantId, e.ReportType })
            .HasDatabaseName("IX_SavedReports_Tenant_Type");
    }
}
