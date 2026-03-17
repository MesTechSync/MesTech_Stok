namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// Basitlestirilmis KDV raporu — API tuketicileri icin.
/// Detayli KDV beyanname taslagi icin KdvDeclarationDraftDto kullanilir.
/// </summary>
public class KdvReportDto
{
    public int Year { get; set; }
    public int Month { get; set; }

    /// <summary>Hesaplanan KDV (output, 391 hesabi).</summary>
    public decimal HesaplananKdv { get; set; }

    /// <summary>Indirilecek KDV (input, 191 hesabi).</summary>
    public decimal IndirilecekKdv { get; set; }

    /// <summary>Odenecek KDV = HesaplananKdv - IndirilecekKdv.</summary>
    public decimal OdenecekKdv { get; set; }

    /// <summary>
    /// Beyanname son teslim tarihi — ayin 26'si.
    /// Turk vergi mevzuati: KDV beyannamesi takip eden ayin 26'sina kadar verilir.
    /// </summary>
    public DateTime BeyannameSonTarih { get; set; }
}
