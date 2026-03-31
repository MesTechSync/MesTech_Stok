using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.EInvoice.Commands;

public sealed class SendEInvoiceHandler : IRequestHandler<SendEInvoiceCommand, bool>
{
    private readonly IEInvoiceDocumentRepository _repository;
    private readonly IEInvoiceProvider _eInvoiceProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendEInvoiceHandler> _logger;

    public SendEInvoiceHandler(
        IEInvoiceDocumentRepository repository,
        IEInvoiceProvider eInvoiceProvider,
        IUnitOfWork unitOfWork,
        ILogger<SendEInvoiceHandler> logger)
    {
        _repository = repository;
        _eInvoiceProvider = eInvoiceProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> Handle(SendEInvoiceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var doc = await _repository.GetByIdAsync(request.EInvoiceId, cancellationToken).ConfigureAwait(false);
        if (doc is null)
        {
            _logger.LogWarning("EInvoice {Id} bulunamadi — send islemi atlanadi.", request.EInvoiceId);
            return false;
        }

        var result = await _eInvoiceProvider.SendAsync(doc, cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            _logger.LogError("EInvoice {Id} gonderilemedi: {Error}", request.EInvoiceId, result.ErrorMessage);
            return false;
        }

        doc.MarkAsSent(result.ProviderRef, result.CreditUsed);
        await _repository.UpdateAsync(doc, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("EInvoice {Id} basariyla gonderildi. ProviderRef={Ref}", request.EInvoiceId, result.ProviderRef);
        return true;
    }
}
