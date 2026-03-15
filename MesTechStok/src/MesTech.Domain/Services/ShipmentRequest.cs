using MesTech.Domain.Enums;

namespace MesTech.Domain.Services;

/// <summary>
/// Kargo atama talebi — siparis bilgilerini tasir.
/// </summary>
public record ShipmentRequest(
    string DestinationCity,
    decimal WeightKg,
    decimal Desi,
    bool IsCashOnDelivery,
    PlatformType? SourcePlatform = null,
    decimal? OrderAmount = null);
