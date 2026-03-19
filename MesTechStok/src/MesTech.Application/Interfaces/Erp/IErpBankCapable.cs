using MesTech.Application.DTOs.ERP;

namespace MesTech.Application.Interfaces.Erp;

/// <summary>
/// ERP banka yetkinligi — banka hareketi sorgulama ve odeme kaydi destekleyen adapter'lar implement eder.
/// </summary>
public interface IErpBankCapable
{
    Task<List<ErpBankTransaction>> GetTransactionsAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<ErpPaymentResult> RecordPaymentAsync(ErpPaymentRequest request, CancellationToken ct = default);
}
