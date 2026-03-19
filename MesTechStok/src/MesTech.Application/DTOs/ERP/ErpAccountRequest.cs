namespace MesTech.Application.DTOs.ERP;

/// <summary>
/// ERP cari hesap olusturma/guncelleme istegi.
/// </summary>
public record ErpAccountRequest(
    string AccountCode,
    string CompanyName,
    string? TaxId,
    string? TaxOffice,
    string? Address,
    string? City,
    string? Phone,
    string? Email
);
