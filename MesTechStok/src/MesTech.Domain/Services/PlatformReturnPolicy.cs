using MesTech.Domain.Enums;

namespace MesTech.Domain.Services;

/// <summary>
/// Platform-specific iade kuralları — config driven.
/// Her platform için iade süresi, kargo bedava mı, alt-sipariş desteği vb.
/// </summary>
public record PlatformReturnPolicy
{
    public PlatformType Platform { get; init; }
    public int ReturnWindowDays { get; init; }
    public bool IsCargoFree { get; init; }
    public bool SupportsSubOrderReturn { get; init; }
    public bool RequiresApproval { get; init; } = true;
    public bool AutoRestoreStock { get; init; } = true;
    public string? ContractedCargoCompany { get; init; }

    /// <summary>
    /// Varsayılan platform politikaları.
    /// Runtime'da appsettings'ten override edilebilir.
    /// </summary>
    public static IReadOnlyDictionary<PlatformType, PlatformReturnPolicy> Defaults { get; } =
        new Dictionary<PlatformType, PlatformReturnPolicy>
        {
            [PlatformType.Trendyol] = new()
            {
                Platform = PlatformType.Trendyol,
                ReturnWindowDays = 15,
                IsCargoFree = true,
                SupportsSubOrderReturn = false,
                RequiresApproval = false,
                AutoRestoreStock = true
            },
            [PlatformType.Ciceksepeti] = new()
            {
                Platform = PlatformType.Ciceksepeti,
                ReturnWindowDays = 14,
                IsCargoFree = false,
                SupportsSubOrderReturn = true,
                RequiresApproval = true,
                AutoRestoreStock = true,
                ContractedCargoCompany = "Anlaşmalı Kargo"
            },
            [PlatformType.Hepsiburada] = new()
            {
                Platform = PlatformType.Hepsiburada,
                ReturnWindowDays = 15,
                IsCargoFree = true,
                SupportsSubOrderReturn = true,
                RequiresApproval = true,
                AutoRestoreStock = true
            },
            [PlatformType.OpenCart] = new()
            {
                Platform = PlatformType.OpenCart,
                ReturnWindowDays = 30,
                IsCargoFree = false,
                SupportsSubOrderReturn = false,
                RequiresApproval = true,
                AutoRestoreStock = false
            },
            [PlatformType.Pazarama] = new()
            {
                Platform = PlatformType.Pazarama,
                ReturnWindowDays = 14,
                IsCargoFree = true,
                SupportsSubOrderReturn = false,
                RequiresApproval = true,
                AutoRestoreStock = true
            }
        };
}
