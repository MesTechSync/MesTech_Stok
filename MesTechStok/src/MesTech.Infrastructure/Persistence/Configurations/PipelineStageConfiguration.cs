using MesTech.Domain.Entities.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class PipelineStageConfiguration : IEntityTypeConfiguration<PipelineStage>
{
    public void Configure(EntityTypeBuilder<PipelineStage> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Color).HasMaxLength(20);
        builder.Property(e => e.Probability).HasPrecision(5, 2);

        builder.HasOne(e => e.Pipeline)
            .WithMany(p => p.Stages)
            .HasForeignKey(e => e.PipelineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.TenantId, e.PipelineId, e.Position })
            .HasDatabaseName("IX_PipelineStages_Tenant_Pipeline_Position");
    }
}
