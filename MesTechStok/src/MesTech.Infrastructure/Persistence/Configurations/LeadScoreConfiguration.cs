using MesTech.Domain.Entities.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class LeadScoreConfiguration : IEntityTypeConfiguration<LeadScore>
{
    public void Configure(EntityTypeBuilder<LeadScore> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.ScoreBreakdownJson).HasColumnType("text");

        builder.HasIndex(l => l.TenantId).HasDatabaseName("IX_LeadScores_TenantId");
        builder.HasIndex(l => new { l.TenantId, l.LeadId })
            .IsUnique()
            .HasDatabaseName("UX_LeadScores_Tenant_Lead");
        builder.HasIndex(l => new { l.TenantId, l.Temperature })
            .HasDatabaseName("IX_LeadScores_Tenant_Temperature");
    }
}
