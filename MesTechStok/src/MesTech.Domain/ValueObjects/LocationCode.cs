namespace MesTech.Domain.ValueObjects;

/// <summary>
/// Depo lokasyon adresleme value object.
/// Format: Zone-Rack-Shelf-Bin (örn: A-01-03-05)
/// </summary>
public record LocationCode
{
    public string? Zone { get; }
    public string? Rack { get; }
    public string? Shelf { get; }
    public string? Bin { get; }

    public LocationCode(string? zone = null, string? rack = null, string? shelf = null, string? bin = null)
    {
        Zone = zone;
        Rack = rack;
        Shelf = shelf;
        Bin = bin;
    }

    public string FullCode
    {
        get
        {
            var parts = new[] { Zone, Rack, Shelf, Bin }
                .Where(p => !string.IsNullOrWhiteSpace(p));
            return string.Join("-", parts);
        }
    }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Zone) &&
                           string.IsNullOrWhiteSpace(Rack) &&
                           string.IsNullOrWhiteSpace(Shelf) &&
                           string.IsNullOrWhiteSpace(Bin);

    public override string ToString() => FullCode;
}
