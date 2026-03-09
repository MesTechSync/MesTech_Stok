using MesTech.Application.DTOs.Invoice;

namespace MesTech.Application.Interfaces;

public interface IKontorCapable
{
    Task<KontorInfo> GetKontorBalanceAsync(CancellationToken ct = default);
}
