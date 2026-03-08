namespace MesTech.Domain.Enums;

/// <summary>
/// Kargo saglayici tipleri.
/// TEMP: DEV 1 domain merge sonrasi kanonik enum ile degistirilecek.
/// </summary>
public enum CargoProvider
{
    None = 0,
    YurticiKargo = 1,
    ArasKargo = 2,
    SuratKargo = 3
}

/// <summary>
/// Kargo gonderi durumlari.
/// </summary>
public enum CargoStatus
{
    Unknown = 0,
    Created = 1,
    PickedUp = 2,
    InTransit = 3,
    OutForDelivery = 4,
    Delivered = 5,
    Returned = 6,
    Cancelled = 7
}
