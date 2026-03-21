using MediatR;
using MesTech.Application.Features.Invoice.DTOs;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Invoice.Queries;

public class GetInvoiceProvidersHandler : IRequestHandler<GetInvoiceProvidersQuery, List<InvoiceProviderStatusDto>>
{
    private static readonly IReadOnlyList<string> AllTypes = new[] { "EFatura", "EArsiv", "EIhracat" };
    private static readonly IReadOnlyList<string> BasicTypes = new[] { "EFatura", "EArsiv" };

    public Task<List<InvoiceProviderStatusDto>> Handle(GetInvoiceProvidersQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var providers = new List<InvoiceProviderStatusDto>
        {
            new(InvoiceProvider.Sovos, "Sovos (Foriba)", IsConfigured: true, IsActive: true, IsReal: true,
                LastTestResult: "OK", LastTestedAt: DateTime.UtcNow.AddHours(-1), SupportedTypes: AllTypes),

            new(InvoiceProvider.Parasut, "Parasut", IsConfigured: true, IsActive: true, IsReal: true,
                LastTestResult: "OK", LastTestedAt: DateTime.UtcNow.AddHours(-2), SupportedTypes: BasicTypes),

            new(InvoiceProvider.GibPortal, "GIB Portal", IsConfigured: true, IsActive: false, IsReal: false,
                LastTestResult: "Mock — sandbox only", LastTestedAt: DateTime.UtcNow.AddDays(-1), SupportedTypes: AllTypes),

            new(InvoiceProvider.ELogo, "e-Logo", IsConfigured: false, IsActive: false, IsReal: false,
                LastTestResult: null, LastTestedAt: null, SupportedTypes: AllTypes),

            new(InvoiceProvider.TrendyolEFaturam, "Trendyol e-Faturam", IsConfigured: false, IsActive: false, IsReal: false,
                LastTestResult: null, LastTestedAt: null, SupportedTypes: BasicTypes),

            new(InvoiceProvider.BirFatura, "BirFatura", IsConfigured: false, IsActive: false, IsReal: false,
                LastTestResult: null, LastTestedAt: null, SupportedTypes: AllTypes),

            new(InvoiceProvider.DijitalPlanet, "Dijital Planet", IsConfigured: false, IsActive: false, IsReal: false,
                LastTestResult: null, LastTestedAt: null, SupportedTypes: AllTypes),

            new(InvoiceProvider.HepsiburadaFatura, "Hepsiburada Fatura", IsConfigured: false, IsActive: false, IsReal: false,
                LastTestResult: null, LastTestedAt: null, SupportedTypes: BasicTypes),

            new(InvoiceProvider.Manual, "Manuel Giris", IsConfigured: true, IsActive: true, IsReal: false,
                LastTestResult: "N/A", LastTestedAt: null, SupportedTypes: AllTypes),
        };

        return Task.FromResult(providers);
    }
}
