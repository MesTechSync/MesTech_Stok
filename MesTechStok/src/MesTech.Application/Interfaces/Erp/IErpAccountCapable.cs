using MesTech.Application.DTOs.ERP;

namespace MesTech.Application.Interfaces.Erp;

/// <summary>
/// ERP hesap yetkinligi — cari hesap CRUD ve bakiye sorgulama destekleyen adapter'lar implement eder.
/// </summary>
public interface IErpAccountCapable
{
    Task<ErpAccountResult> CreateAccountAsync(ErpAccountRequest request, CancellationToken ct = default);
    Task<ErpAccountResult?> GetAccountAsync(string accountCode, CancellationToken ct = default);
    Task<ErpAccountResult> UpdateAccountAsync(ErpAccountRequest request, CancellationToken ct = default);
    Task<List<ErpAccountResult>> SearchAccountsAsync(string query, CancellationToken ct = default);
    Task<decimal> GetAccountBalanceAsync(string accountCode, CancellationToken ct = default);
}
