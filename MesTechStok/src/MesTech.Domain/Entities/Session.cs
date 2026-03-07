using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

public class Session : BaseEntity
{
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceInfo { get; set; }
    public DateTime? LastActivity { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsValid => IsActive && !IsExpired;
}
