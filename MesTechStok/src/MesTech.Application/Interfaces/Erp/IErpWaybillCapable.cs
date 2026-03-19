using MesTech.Application.DTOs.ERP;

namespace MesTech.Application.Interfaces.Erp;

/// <summary>
/// ERP irsaliye yetkinligi — irsaliye olusturma ve sorgulama destekleyen adapter'lar implement eder.
/// </summary>
public interface IErpWaybillCapable
{
    Task<ErpWaybillResult> CreateWaybillAsync(ErpWaybillRequest request, CancellationToken ct = default);
    Task<ErpWaybillResult?> GetWaybillAsync(string waybillNumber, CancellationToken ct = default);
}
