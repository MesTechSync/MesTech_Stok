using MesTech.Domain.Common;

namespace MesTech.Domain.Entities;

/// <summary>
/// Mağaza API credential'ları — AES-256-GCM ile şifrelenmiş.
/// </summary>
public class StoreCredential : BaseEntity
{
    public Guid StoreId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string EncryptedValue { get; set; } = string.Empty;

    // Navigation
    public Store Store { get; set; } = null!;
}
