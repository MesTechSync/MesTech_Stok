namespace MesTech.Domain.ValueObjects;

/// <summary>
/// Adres value object — kargo gonderi/alici adresi.
/// </summary>
public record Address
{
    public string Street { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = "TR";

    public string FullAddress => $"{Street}, {District}, {City} {PostalCode}, {Country}";
}
