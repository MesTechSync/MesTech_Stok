namespace MesTech.Domain.Enums;

/// <summary>
/// Dijital imza durumu.
/// </summary>
#pragma warning disable CA1720 // Identifier 'Unsigned'/'Signed' — e-fatura domain terminology
public enum SignatureStatus
{
    Unsigned = 0,
    Signed = 1,
    Invalid = 2,
    Expired = 3,
    Revoked = 4
}
