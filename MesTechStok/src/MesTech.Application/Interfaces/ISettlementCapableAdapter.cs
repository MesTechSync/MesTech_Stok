using MesTech.Application.DTOs;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Cari hesap / muhasebe destekleyen platform adaptörleri icin interface.
/// </summary>
public interface ISettlementCapableAdapter
{
    Task<SettlementDto?> GetSettlementAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
    Task<IReadOnlyList<CargoInvoiceDto>> GetCargoInvoicesAsync(DateTime startDate, CancellationToken ct = default);
}
