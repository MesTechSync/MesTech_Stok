using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Invoice.Commands;

public class ApproveInvoiceHandler : IRequestHandler<ApproveInvoiceCommand, bool>
{
    private readonly IInvoiceRepository _repository;
    private readonly ILogger<ApproveInvoiceHandler> _logger;

    public ApproveInvoiceHandler(IInvoiceRepository repository, ILogger<ApproveInvoiceHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> Handle(ApproveInvoiceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var invoice = await _repository.GetByIdAsync(request.InvoiceId);
        if (invoice is null)
        {
            _logger.LogWarning("Invoice {Id} bulunamadi — approve islemi atlanadi.", request.InvoiceId);
            return false;
        }

        try
        {
            invoice.Approve();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Invoice {Id} onaylanamadi: {Error}", request.InvoiceId, ex.Message);
            return false;
        }

        await _repository.UpdateAsync(invoice);

        _logger.LogInformation("Invoice {Id} ({Number}) basariyla onaylandi.", request.InvoiceId, invoice.InvoiceNumber);
        return true;
    }
}
