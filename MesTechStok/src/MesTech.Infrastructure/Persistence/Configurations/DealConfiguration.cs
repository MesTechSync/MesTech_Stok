using MesTech.Domain.Entities.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Title).HasMaxLength(300);
        builder.Property(e => e.Currency).HasMaxLength(10);
        builder.Property(e => e.LostReason).HasMaxLength(500);
        builder.Property(e => e.Amount).HasPrecision(18, 4);

        builder.HasOne(e => e.Pipeline)
            .WithMany()
            .HasForeignKey(e => e.PipelineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Stage)
            .WithMany()
            .HasForeignKey(e => e.StageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_Deals_Tenant_Status");

        builder.HasIndex(e => new { e.TenantId, e.PipelineId, e.StageId })
            .HasDatabaseName("IX_Deals_Tenant_Pipeline_Stage");

        builder.HasIndex(e => new { e.TenantId, e.AssignedToUserId })
            .HasDatabaseName("IX_Deals_Tenant_Assigned");

        // Explicit FK — prevents EF Core from creating shadow CrmContactId1
        builder.HasOne(e => e.Contact)
            .WithMany()
            .HasForeignKey(e => e.CrmContactId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
