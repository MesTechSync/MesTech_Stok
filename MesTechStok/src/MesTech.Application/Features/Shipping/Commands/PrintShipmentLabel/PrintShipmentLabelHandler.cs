using MediatR;

namespace MesTech.Application.Features.Shipping.Commands.PrintShipmentLabel;

/// <summary>
/// Kargo etiketi yazdirma handler'i.
/// Stub: Gercek yazici entegrasyonu Infrastructure katmaninda (IPrinterService) yapilacak.
/// </summary>
public sealed class PrintShipmentLabelHandler : IRequestHandler<PrintShipmentLabelCommand, PrintShipmentLabelResult>
{
    public Task<PrintShipmentLabelResult> Handle(
        PrintShipmentLabelCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Stub — Infrastructure concern: IPrinterService.PrintLabelAsync()
        var result = new PrintShipmentLabelResult
        {
            IsSuccess = true,
            ErrorMessage = null
        };

        return Task.FromResult(result);
    }
}
