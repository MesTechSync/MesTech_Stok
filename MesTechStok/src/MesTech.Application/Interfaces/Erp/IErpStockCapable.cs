using MesTech.Application.DTOs.ERP;

namespace MesTech.Application.Interfaces.Erp;

/// <summary>
/// ERP stok yetkinligi — stok seviyesi sorgulama ve guncelleme destekleyen adapter'lar implement eder.
/// </summary>
public interface IErpStockCapable
{
    Task<List<ErpStockItem>> GetStockLevelsAsync(CancellationToken ct = default);
    Task<ErpStockItem?> GetStockByCodeAsync(string productCode, CancellationToken ct = default);
    Task<bool> UpdateStockAsync(string productCode, int quantity, string warehouseCode, CancellationToken ct = default);
}
