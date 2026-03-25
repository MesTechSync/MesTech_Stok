using MediatR;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Features.EInvoice.Queries;

public sealed class CheckVknMukellefHandler : IRequestHandler<CheckVknMukellefQuery, VknMukellefResult>
{
    private readonly IEInvoiceProvider _eInvoiceProvider;

    public CheckVknMukellefHandler(IEInvoiceProvider eInvoiceProvider)
    {
        _eInvoiceProvider = eInvoiceProvider;
    }

    public Task<VknMukellefResult> Handle(CheckVknMukellefQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _eInvoiceProvider.CheckVknMukellefAsync(request.Vkn, cancellationToken);
    }
}
