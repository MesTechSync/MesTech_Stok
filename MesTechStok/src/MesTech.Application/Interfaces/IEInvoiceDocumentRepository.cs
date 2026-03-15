using MesTech.Domain.Entities.EInvoice;
using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

public interface IEInvoiceDocumentRepository
{
    Task<EInvoiceDocument?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EInvoiceDocument?> GetByEttnNoAsync(string ettnNo, CancellationToken ct = default);
    Task<EInvoiceDocument?> GetByGibUuidAsync(string gibUuid, CancellationToken ct = default);
    Task<(IReadOnlyList<EInvoiceDocument> Items, int Total)> GetPagedAsync(
        EInvoiceStatus? status = null,
        DateTime? from = null,
        DateTime? to = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);
    Task AddAsync(EInvoiceDocument doc, CancellationToken ct = default);
    Task UpdateAsync(EInvoiceDocument doc, CancellationToken ct = default);
}
