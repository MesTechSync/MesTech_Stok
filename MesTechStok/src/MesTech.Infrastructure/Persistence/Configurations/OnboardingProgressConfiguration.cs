using MesTech.Domain.Entities.Onboarding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class OnboardingProgressConfiguration : IEntityTypeConfiguration<OnboardingProgress>
{
    public void Configure(EntityTypeBuilder<OnboardingProgress> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("onboarding_progress");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompletedStepsJson).HasColumnType("jsonb");

        builder.HasIndex(x => x.TenantId).IsUnique();
        builder.HasIndex(x => x.IsCompleted);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
