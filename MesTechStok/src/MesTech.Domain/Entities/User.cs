using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Kullanıcı entity'si.
/// Tenant hiyerarşisi: Tenant → Store → User.
/// ITenantEntity — multi-tenant query filter ile korunur.
/// </summary>
public sealed class User : BaseEntity, ITenantEntity
{
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsEmailConfirmed { get; set; }
    public DateTime? LastLoginDate { get; set; }

    // Tenant ilişkisi — ITenantEntity zorunlu. Global query filter otomatik uygulanır.
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string? FullName => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
        ? Username
        : $"{FirstName} {LastName}".Trim();

    // Navigation
    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
}
