namespace MesTech.Domain.Enums;

/// <summary>
/// XAdES dijital imza tipi (GİB uyumlu).
/// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores — XAdES standard naming
#pragma warning disable CA1008 // Enums should have zero value — XAdES types start from 1 (no "None" concept)
public enum SignatureType
{
    XAdES_BES = 1,     // Temel elektronik imza (minimum GİB gereksinimi)
    XAdES_T = 2,       // Zaman damgalı (Time-stamped)
    XAdES_XL = 3       // Uzun vadeli (Long-term)
}
