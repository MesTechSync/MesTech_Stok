namespace MesTech.Domain.Enums;

/// <summary>
/// KDV orani — fatura kalemlerinde kullanilir.
/// Deger = gercek yuzde (parse kolayligi).
/// </summary>
public enum KdvRate
{
    Yuzde0 = 0,
    Yuzde1 = 1,
    Yuzde10 = 10,
    Yuzde20 = 20
}
