using MesTech.Application.DTOs.ERP;

namespace MesTech.Application.Interfaces.Erp;

/// <summary>
/// ERP fiyat yetkinligi — urun fiyat sorgulama destekleyen adapter'lar implement eder.
/// </summary>
public interface IErpPriceCapable
{
    Task<List<ErpPriceItem>> GetProductPricesAsync(CancellationToken ct = default);
    Task<ErpPriceItem?> GetPriceByCodeAsync(string productCode, CancellationToken ct = default);
}
