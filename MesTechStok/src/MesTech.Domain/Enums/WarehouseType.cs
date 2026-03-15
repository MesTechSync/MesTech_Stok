namespace MesTech.Domain.Enums;

/// <summary>
/// Depo türleri.
/// </summary>
public enum WarehouseType
{
    Main,
    Branch,
    Transit,
    Return,
    Quarantine,

    /// <summary>Amazon FBA (Fulfillment by Amazon) deposu.</summary>
    AmazonFBA,

    /// <summary>Hepsilojistik (Hepsiburada fulfillment) deposu.</summary>
    Hepsilojistik
}
