using MediatR;

namespace MesTech.Application.Features.Onboarding.Commands.RegisterTenant;

/// <summary>
/// Tam kayıt akışı — tek atomik işlemde:
/// 1. Tenant oluştur
/// 2. Admin kullanıcı oluştur (BCrypt hash)
/// 3. Trial subscription başlat (14 gün)
/// 4. Onboarding progress başlat
/// </summary>
public record RegisterTenantCommand(
    string CompanyName,
    string? TaxNumber,
    string AdminUsername,
    string AdminEmail,
    string AdminPassword,
    string? AdminFirstName = null,
    string? AdminLastName = null
) : IRequest<RegisterTenantResult>;

public sealed class RegisterTenantResult
{
    public Guid TenantId { get; init; }
    public Guid AdminUserId { get; init; }
    public Guid SubscriptionId { get; init; }
    public Guid OnboardingId { get; init; }
    public DateTime TrialEndsAt { get; init; }
    public string PlanName { get; init; } = string.Empty;
}
