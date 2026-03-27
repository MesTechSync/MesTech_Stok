using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class StockAlertRuleConfiguration : IEntityTypeConfiguration<StockAlertRule>
{
    public void Configure(EntityTypeBuilder<StockAlertRule> builder)
    {
        builder.ToTable("StockAlertRules");

        builder.HasIndex(x => new { x.TenantId, x.ProductId, x.WarehouseId })
            .IsUnique()
            .HasDatabaseName("IX_StockAlertRules_Tenant_Product_Warehouse");

        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
