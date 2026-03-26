using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class ProcessedDomainEventConfiguration : IEntityTypeConfiguration<ProcessedDomainEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedDomainEvent> builder)
    {
        builder.ToTable("ProcessedDomainEvents");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.EventId, e.HandlerName })
            .IsUnique()
            .HasDatabaseName("IX_ProcessedDomainEvents_EventId_Handler");

        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_ProcessedDomainEvents_TenantId");

        builder.Property(e => e.EventType).HasMaxLength(256).IsRequired();
        builder.Property(e => e.HandlerName).HasMaxLength(256);
    }
}
