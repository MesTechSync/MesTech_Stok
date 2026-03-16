namespace MesTech.Domain.Enums;

/// <summary>
/// GIB e-Fatura senaryosu (UBL-TR 1.2.1 ProfileID).
/// TEMELFATURA: Temel fatura — alici kabul/red yapamaz.
/// TICARIFATURA: Ticari fatura — alici kabul/red/iade yapabilir.
/// EARSIVFATURA: e-Arsiv fatura — e-Fatura mukellefiyeti olmayan alicilara kesilir.
/// </summary>
public enum EInvoiceScenario
{
    TEMELFATURA = 0,
    TICARIFATURA = 1,
    EARSIVFATURA = 2
}
