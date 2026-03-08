namespace MesTech.Application.DTOs.Cargo;

/// <summary>
/// Kargo etiketi sonucu — adapter'dan donen etiket verisi + format bilgisi.
/// </summary>
public sealed class LabelResult
{
    public required byte[] Data { get; init; }
    public required LabelFormat Format { get; init; }
    public required string FileName { get; init; }
}

/// <summary>
/// Etiket format tipleri.
/// </summary>
public enum LabelFormat
{
    Pdf = 0,
    Zpl = 1,
    Png = 2
}
