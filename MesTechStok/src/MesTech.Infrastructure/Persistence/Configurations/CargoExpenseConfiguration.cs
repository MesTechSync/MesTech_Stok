using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class CargoExpenseConfiguration : IEntityTypeConfiguration<CargoExpense>
{
    public void Configure(EntityTypeBuilder<CargoExpense> builder)
    {
        builder.Property(x => x.OrderId).HasMaxLength(100);
        builder.Property(x => x.CarrierName).HasMaxLength(100);
        builder.Property(x => x.TrackingNumber).HasMaxLength(100);
        builder.Property(x => x.Cost).HasPrecision(18, 2);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.OrderId);
    }
}
