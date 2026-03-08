namespace MesTech.Domain.Enums;

/// <summary>
/// Desteklenen kargo saglayici firmalari.
/// Kanonik Domain enum — tum cargo adapter'lar bu enum'u kullanir.
/// </summary>
public enum CargoProvider
{
    None = 0,
    YurticiKargo = 1,
    ArasKargo = 2,
    SuratKargo = 3,
    MngKargo = 4,
    PttKargo = 5,
    Hepsijet = 6,
    UPS = 7
}

/// <summary>
/// Kargo gonderi durumlari.
/// </summary>
public enum CargoStatus
{
    Created = 0,
    PickedUp = 1,
    InTransit = 2,
    OutForDelivery = 3,
    Delivered = 4,
    Returned = 5,
    Lost = 6,
    Cancelled = 7,
    AtBranch = 8
}
