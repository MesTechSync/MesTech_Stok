using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class PersonalDataRetentionPolicyConfiguration : IEntityTypeConfiguration<PersonalDataRetentionPolicy>
{
    public void Configure(EntityTypeBuilder<PersonalDataRetentionPolicy> builder)
    {
        builder.ToTable("PersonalDataRetentionPolicies");

        builder.HasIndex(x => x.EntityTypeName)
            .IsUnique()
            .HasDatabaseName("IX_RetentionPolicies_EntityType");

        builder.Property(x => x.EntityTypeName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AnonymizationStrategy).HasMaxLength(50).IsRequired();
        builder.Property(x => x.FieldsToAnonymize).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        // Seed: DataRetentionJob runtime'da policy yoksa hardcoded fallback kullanır.
        // Migration-safe seed için EnsureDefaultPoliciesAsync() kullanılacak.

        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_personal_data_retention_policies_tenant_id");
    }
}
