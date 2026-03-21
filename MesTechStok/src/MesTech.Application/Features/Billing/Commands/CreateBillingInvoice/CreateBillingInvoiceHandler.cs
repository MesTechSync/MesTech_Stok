using MediatR;
using MesTech.Domain.Entities.Billing;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Billing.Commands.CreateBillingInvoice;

public class CreateBillingInvoiceHandler : IRequestHandler<CreateBillingInvoiceCommand, Guid>
{
    private readonly IBillingInvoiceRepository _invoiceRepo;
    private readonly IUnitOfWork _uow;

    public CreateBillingInvoiceHandler(IBillingInvoiceRepository invoiceRepo, IUnitOfWork uow)
        => (_invoiceRepo, _uow) = (invoiceRepo, uow);

    public async Task<Guid> Handle(CreateBillingInvoiceCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var seq = await _invoiceRepo.GetNextSequenceAsync(cancellationToken);
        var invoiceNumber = BillingInvoice.GenerateInvoiceNumber(seq);

        var invoice = BillingInvoice.Create(
            request.TenantId, request.SubscriptionId, invoiceNumber,
            request.Amount, request.CurrencyCode, request.TaxRate, request.DueDays);

        await _invoiceRepo.AddAsync(invoice, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return invoice.Id;
    }
}
