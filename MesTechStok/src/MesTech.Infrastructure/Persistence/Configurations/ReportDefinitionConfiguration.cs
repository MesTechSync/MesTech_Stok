using MesTech.Domain.Entities.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.Property(r => r.FilterJson).HasColumnType("text");
        builder.Property(r => r.RecipientEmail).HasMaxLength(256);
        builder.Property(r => r.Notes).HasMaxLength(2000);

        builder.HasIndex(r => r.TenantId).HasDatabaseName("IX_ReportDefinitions_TenantId");
        builder.HasIndex(r => new { r.TenantId, r.Type, r.IsActive })
            .HasDatabaseName("IX_ReportDefinitions_Tenant_Type_Active");
    }
}
