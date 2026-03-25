namespace MesTech.Application.DTOs.Platform;

/// <summary>
/// Platform-agnostic musteri senkronizasyon DTO'su.
/// Tum platformlarda ortak musteri alanlari.
/// </summary>
public sealed class CustomerSyncDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public DateTime DateModified { get; set; }
}
