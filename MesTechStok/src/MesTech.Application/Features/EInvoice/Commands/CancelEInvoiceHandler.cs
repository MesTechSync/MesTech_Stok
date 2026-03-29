using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.EInvoice.Commands;

public sealed class CancelEInvoiceHandler : IRequestHandler<CancelEInvoiceCommand, bool>
{
    private readonly IEInvoiceDocumentRepository _repository;
    private readonly IEInvoiceProvider _eInvoiceProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CancelEInvoiceHandler(
        IEInvoiceDocumentRepository repository,
        IEInvoiceProvider eInvoiceProvider,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _eInvoiceProvider = eInvoiceProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(CancelEInvoiceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var doc = await _repository.GetByIdAsync(request.EInvoiceId, cancellationToken);
        if (doc is null)
            return false;

        // If already sent with a provider reference, cancel at provider first
        if (!string.IsNullOrWhiteSpace(doc.ProviderRef))
        {
            var cancelled = await _eInvoiceProvider.CancelAsync(doc.ProviderRef, request.Reason, cancellationToken);
            if (!cancelled)
                return false;
        }

        doc.Cancel(request.Reason, cancelledBy: "system");
        await _repository.UpdateAsync(doc, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
