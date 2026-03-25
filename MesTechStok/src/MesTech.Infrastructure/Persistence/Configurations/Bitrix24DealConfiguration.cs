using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class Bitrix24DealConfiguration : IEntityTypeConfiguration<Bitrix24Deal>
{
    public void Configure(EntityTypeBuilder<Bitrix24Deal> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.ExternalDealId).HasMaxLength(100);
        builder.Property(d => d.Title).HasMaxLength(500);
        builder.Property(d => d.StageId).HasMaxLength(50);
        builder.Property(d => d.Currency).HasMaxLength(10).HasDefaultValue("TRY");
        builder.Property(d => d.Opportunity).HasPrecision(18, 2);
        builder.Property(d => d.SyncError).HasMaxLength(2000);

        builder.HasIndex(d => d.OrderId);
        builder.HasIndex(d => d.ExternalDealId);
        builder.HasIndex(d => d.TenantId);

        builder.HasOne(d => d.Order)
            .WithMany()
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.ProductRows)
            .WithOne()
            .HasForeignKey(r => r.Bitrix24DealId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
