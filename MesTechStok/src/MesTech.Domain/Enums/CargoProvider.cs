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
    UPS = 7,
    Sendeo = 8,
    DHL = 9,
    FedEx = 10
}

