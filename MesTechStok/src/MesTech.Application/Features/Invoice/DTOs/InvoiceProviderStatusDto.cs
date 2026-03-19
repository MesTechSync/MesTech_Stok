using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Invoice.DTOs;

public record InvoiceProviderStatusDto(
    InvoiceProvider Provider,
    string Name,
    bool IsConfigured,
    bool IsActive,
    bool IsReal,
    string? LastTestResult,
    DateTime? LastTestedAt,
    IReadOnlyList<string> SupportedTypes);
