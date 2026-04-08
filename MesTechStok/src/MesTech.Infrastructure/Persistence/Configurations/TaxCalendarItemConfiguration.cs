using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class TaxCalendarItemConfiguration : IEntityTypeConfiguration<TaxCalendarItem>
{
    public void Configure(EntityTypeBuilder<TaxCalendarItem> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TaxType).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.Property(t => t.Notes).HasMaxLength(2000);

        builder.HasIndex(t => t.TenantId).HasDatabaseName("IX_TaxCalendarItems_TenantId");
        builder.HasIndex(t => new { t.TenantId, t.TaxType, t.Frequency })
            .HasDatabaseName("IX_TaxCalendarItems_Tenant_Type");
    }
}
