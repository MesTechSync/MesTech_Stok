using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.SendInvoice;

public sealed class SendInvoiceHandler : IRequestHandler<SendInvoiceCommand, SendInvoiceResult>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendInvoiceHandler> _logger;

    public SendInvoiceHandler(
        IInvoiceRepository invoiceRepository,
        IUnitOfWork unitOfWork,
        ILogger<SendInvoiceHandler> logger)
    {
        _invoiceRepository = invoiceRepository ?? throw new ArgumentNullException(nameof(invoiceRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SendInvoiceResult> Handle(SendInvoiceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken).ConfigureAwait(false);
        if (invoice == null)
            return new SendInvoiceResult { IsSuccess = false, ErrorMessage = $"Invoice {request.InvoiceId} not found." };

        try
        {
            // Mark invoice as sent — actual e-invoice provider dispatch handled by domain events / consumers
            invoice.MarkAsSent(gibInvoiceId: null, pdfUrl: null);
            await _invoiceRepository.UpdateAsync(invoice, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Invoice {InvoiceId} marked as sent", request.InvoiceId);

            return new SendInvoiceResult { IsSuccess = true };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "SendInvoice FAILED — InvoiceId={InvoiceId}. Fatura Sent durumuna geçirilemedi.",
                request.InvoiceId);

            return new SendInvoiceResult
            {
                IsSuccess = false,
                ErrorMessage = $"Fatura gönderimi sırasında hata oluştu: {ex.Message}"
            };
        }
    }
}
