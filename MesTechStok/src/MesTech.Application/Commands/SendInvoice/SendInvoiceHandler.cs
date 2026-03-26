using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.SendInvoice;

public sealed class SendInvoiceHandler : IRequestHandler<SendInvoiceCommand, SendInvoiceResult>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SendInvoiceHandler(IInvoiceRepository invoiceRepository, IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SendInvoiceResult> Handle(SendInvoiceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId).ConfigureAwait(false);
        if (invoice == null)
            return new SendInvoiceResult { IsSuccess = false, ErrorMessage = $"Invoice {request.InvoiceId} not found." };

        // Mark invoice as sent — actual e-invoice provider dispatch handled by domain events / consumers
        invoice.MarkAsSent(gibInvoiceId: null, pdfUrl: null);
        await _invoiceRepository.UpdateAsync(invoice).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new SendInvoiceResult
        {
            IsSuccess = true,
        };
    }
}
