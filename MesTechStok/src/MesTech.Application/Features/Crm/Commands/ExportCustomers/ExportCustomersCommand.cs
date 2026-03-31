using MediatR;

namespace MesTech.Application.Features.Crm.Commands.ExportCustomers;

/// <summary>
/// Musteri verilerini disa aktarma komutu — ExportAvaloniaViewModel.ExportAsync().
/// </summary>
public sealed record ExportCustomersCommand(
    Guid TenantId,
    string Format = "xlsx",
    string? SearchTerm = null
) : IRequest<ExportCustomersResult>;
