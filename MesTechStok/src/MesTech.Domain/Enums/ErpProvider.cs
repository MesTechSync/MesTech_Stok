namespace MesTech.Domain.Enums;

/// <summary>
/// Desteklenen ERP saglayicilari.
/// Kanonik Domain enum — tum ERP adapter'lar bu enum'u kullanir.
/// Dalga 11: ERP entegrasyonu icin eklendi.
/// </summary>
public enum ErpProvider
{
    None = 0,
    Logo = 1,
    Netsis = 2,
    Nebim = 3,
    Parasut = 4,
    BizimHesap = 5,
    Mikro = 6,
    ERPNext = 7
}
