using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// PricingRule entity EF Core configuration — indexes, precision, constraints.
/// </summary>
public sealed class PricingRuleConfiguration : IEntityTypeConfiguration<PricingRule>
{
    public void Configure(EntityTypeBuilder<PricingRule> builder)
    {
        builder.HasIndex(r => new { r.TenantId, r.Platform, r.IsActive })
            .HasDatabaseName("IX_PricingRules_Tenant_Platform_Active");

        builder.HasIndex(r => new { r.TenantId, r.Priority })
            .HasDatabaseName("IX_PricingRules_Tenant_Priority");

        builder.Property(r => r.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.CompetitorOffset).HasPrecision(18, 4);
        builder.Property(r => r.MinMarginPercent).HasPrecision(8, 4);
        builder.Property(r => r.MarkupPercent).HasPrecision(8, 4);
        builder.Property(r => r.MinPrice).HasPrecision(18, 4);
        builder.Property(r => r.MaxPrice).HasPrecision(18, 4);
        builder.Property(r => r.RoundTo).HasPrecision(18, 4);
    }
}
