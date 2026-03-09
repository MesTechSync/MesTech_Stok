using MesTech.Application.DTOs.Invoice;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Kontor bakiye sorgulama capability — kontor bazli entegratorler (Sovos, TrendyolEFaturam).
/// </summary>
public interface IKontorCapable
{
    Task<KontorBalanceDto> GetKontorBalanceAsync(CancellationToken ct = default);
}
