namespace MesTech.Domain.Enums;

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
