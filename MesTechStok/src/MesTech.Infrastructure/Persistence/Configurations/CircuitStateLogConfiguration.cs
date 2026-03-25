using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class CircuitStateLogConfiguration : IEntityTypeConfiguration<CircuitStateLog>
{
    public void Configure(EntityTypeBuilder<CircuitStateLog> builder)
    {
        builder.Property(c => c.PreviousState).HasMaxLength(50);
        builder.Property(c => c.NewState).HasMaxLength(50);
        builder.Property(c => c.Reason).HasMaxLength(500);
        builder.Property(c => c.CorrelationId).HasMaxLength(100);
        builder.Property(c => c.AdditionalInfo).HasMaxLength(2000);

        builder.HasIndex(c => new { c.TenantId, c.TransitionTimeUtc })
            .HasDatabaseName("IX_CircuitStateLogs_Tenant_TransitionTime");
    }
}
