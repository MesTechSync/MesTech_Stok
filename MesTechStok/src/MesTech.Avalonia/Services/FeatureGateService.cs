namespace MesTech.Avalonia.Services;

/// <summary>
/// Subscription tier for MesTech SaaS.
/// Higher tiers are supersets of lower tiers.
/// </summary>
public enum SubscriptionTier
{
    Light = 0,
    Pro   = 1,
    Ultra = 2
}

/// <summary>
/// Controls feature visibility per subscription tier.
/// ViewModels / sidebar builders call IsEnabled() to hide/show UI elements.
/// </summary>
public interface IFeatureGateService
{
    SubscriptionTier CurrentTier { get; }
    bool IsEnabled(string feature);
    void SetTier(SubscriptionTier tier);
    event EventHandler<SubscriptionTier>? TierChanged;
}

public class FeatureGateService : IFeatureGateService
{
    // ── Feature → minimum required tier ────────────────────────────────────────
    private static readonly Dictionary<string, SubscriptionTier> FeatureMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Light (0) — always visible
            { "Dashboard",      SubscriptionTier.Light },
            { "Products",       SubscriptionTier.Light },
            { "Orders",         SubscriptionTier.Light },
            { "Stock",          SubscriptionTier.Light },
            { "Settings",       SubscriptionTier.Light },

            // Pro (1)
            { "Reports",        SubscriptionTier.Pro },
            { "Analytics",      SubscriptionTier.Pro },
            { "CRM",            SubscriptionTier.Pro },
            { "Cargo",          SubscriptionTier.Pro },
            { "Invoice",        SubscriptionTier.Pro },
            { "Export",         SubscriptionTier.Pro },
            { "MultiPlatform",  SubscriptionTier.Pro },

            // Ultra (2)
            { "AIInsight",      SubscriptionTier.Ultra },
            { "MesaBridge",     SubscriptionTier.Ultra },
            { "Automation",     SubscriptionTier.Ultra },
            { "Webhook",        SubscriptionTier.Ultra },
            { "ApiAccess",      SubscriptionTier.Ultra },
            { "LogViewer",      SubscriptionTier.Ultra },
            { "HealthMonitor",  SubscriptionTier.Ultra },
        };

    private SubscriptionTier _currentTier = SubscriptionTier.Ultra; // default for dev/testing

    public SubscriptionTier CurrentTier => _currentTier;

    public event EventHandler<SubscriptionTier>? TierChanged;

    public bool IsEnabled(string feature)
    {
        if (!FeatureMap.TryGetValue(feature, out var required))
            return true; // unknown feature — allow by default (fail-open)

        return _currentTier >= required;
    }

    public void SetTier(SubscriptionTier tier)
    {
        _currentTier = tier;
        TierChanged?.Invoke(this, tier);
    }
}
