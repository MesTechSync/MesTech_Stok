using MediatR;

namespace MesTech.Application.Features.Invoice.Commands.ExportInvoiceReport;

/// <summary>
/// Fatura raporu disa aktarma komutu — InvoiceReportAvaloniaViewModel.ExportExcel/ExportPdf().
/// </summary>
public sealed record ExportInvoiceReportCommand(
    Guid TenantId,
    string Format = "xlsx",
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? Period = null
) : IRequest<ExportInvoiceReportResult>;
