using MediatR;

namespace MesTech.Application.Features.Invoice.Commands.ExportInvoices;

/// <summary>
/// Fatura verilerini disa aktarma komutu — ExportAvaloniaViewModel.ExportAsync().
/// </summary>
public sealed record ExportInvoicesCommand(
    Guid TenantId,
    string Format = "xlsx",
    DateTime? DateFrom = null,
    DateTime? DateTo = null
) : IRequest<ExportInvoicesResult>;
