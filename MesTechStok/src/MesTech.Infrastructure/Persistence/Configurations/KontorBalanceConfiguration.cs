using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class KontorBalanceConfiguration : IEntityTypeConfiguration<KontorBalance>
{
    public void Configure(EntityTypeBuilder<KontorBalance> builder)
    {
        builder.HasIndex(k => new { k.StoreId, k.Provider })
            .IsUnique()
            .HasDatabaseName("IX_KontorBalances_Store_Provider");

        builder.HasIndex(k => k.TenantId)
            .HasDatabaseName("IX_KontorBalances_TenantId");

        builder.HasOne(k => k.Store)
            .WithMany()
            .HasForeignKey(k => k.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
