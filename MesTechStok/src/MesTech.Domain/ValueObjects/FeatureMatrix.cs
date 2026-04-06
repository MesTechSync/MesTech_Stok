using MesTech.Domain.Enums;

namespace MesTech.Domain.ValueObjects;

/// <summary>
/// Plan katmani ozellik matrisi — hangi ozellik hangi planda acik.
/// SubscriptionTier → Limitleri ve ozellik flaglerini doner.
/// </summary>
public sealed record FeatureMatrix
{
    public SubscriptionTier Tier { get; }
    public int MaxPlatforms { get; }
    public int MaxProducts { get; }
    public int MaxUsers { get; }
    public bool AiRecommendations { get; }
    public bool MesaOsIntegration { get; }
    public bool CustomApi { get; }
    public bool WhiteLabel { get; }
    public bool AdvancedReporting { get; }
    public bool MultiWarehouse { get; }
    public bool EInvoice { get; }
    public bool DropshippingPool { get; }

    private FeatureMatrix(SubscriptionTier tier, int maxPlatforms, int maxProducts, int maxUsers,
        bool ai, bool mesa, bool customApi, bool whiteLabel,
        bool advReporting, bool multiWarehouse, bool eInvoice, bool dropshipping)
    {
        Tier = tier;
        MaxPlatforms = maxPlatforms;
        MaxProducts = maxProducts;
        MaxUsers = maxUsers;
        AiRecommendations = ai;
        MesaOsIntegration = mesa;
        CustomApi = customApi;
        WhiteLabel = whiteLabel;
        AdvancedReporting = advReporting;
        MultiWarehouse = multiWarehouse;
        EInvoice = eInvoice;
        DropshippingPool = dropshipping;
    }

    public static FeatureMatrix ForTier(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Light => new(tier,
            maxPlatforms: 3, maxProducts: 500, maxUsers: 1,
            ai: false, mesa: false, customApi: false, whiteLabel: false,
            advReporting: false, multiWarehouse: false, eInvoice: false, dropshipping: false),

        SubscriptionTier.Pro => new(tier,
            maxPlatforms: 10, maxProducts: 5000, maxUsers: 5,
            ai: true, mesa: false, customApi: false, whiteLabel: false,
            advReporting: true, multiWarehouse: true, eInvoice: true, dropshipping: true),

        SubscriptionTier.UltraPro => new(tier,
            maxPlatforms: int.MaxValue, maxProducts: int.MaxValue, maxUsers: int.MaxValue,
            ai: true, mesa: true, customApi: true, whiteLabel: true,
            advReporting: true, multiWarehouse: true, eInvoice: true, dropshipping: true),

        _ => throw new ArgumentOutOfRangeException(nameof(tier))
    };

    public bool IsFeatureEnabled(string featureName) => featureName switch
    {
        "AiRecommendations" => AiRecommendations,
        "MesaOsIntegration" => MesaOsIntegration,
        "CustomApi" => CustomApi,
        "WhiteLabel" => WhiteLabel,
        "AdvancedReporting" => AdvancedReporting,
        "MultiWarehouse" => MultiWarehouse,
        "EInvoice" => EInvoice,
        "DropshippingPool" => DropshippingPool,
        _ => false
    };
}
