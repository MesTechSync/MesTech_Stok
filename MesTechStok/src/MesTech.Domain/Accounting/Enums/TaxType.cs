namespace MesTech.Domain.Accounting.Enums;

/// <summary>
/// Vergi turleri — Turkiye vergi mevzuati.
/// </summary>
public enum TaxType
{
    KDV = 0,
    GelirVergisi = 1,
    KurumlarVergisi = 2,
    DamgaVergisi = 3,
    MTV = 4,
    SGK = 5,
    StopajVergisi = 6,
    OTV = 7,
    Other = 99
}
