namespace MesTech.Application.DTOs.Cargo;

/// <summary>
/// Kargo etiketi sonucu — adapter'dan donen etiket verisi + format bilgisi.
/// </summary>
public sealed class LabelResult
{
    // CA1819: binary label payload — array is intentional (interop with PDF/ZPL libraries)
#pragma warning disable CA1819
    public required byte[] Data { get; init; }
#pragma warning restore CA1819
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
