using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetPlatformCommissionRates;

public class GetPlatformCommissionRatesHandler : IRequestHandler<GetPlatformCommissionRatesQuery, IReadOnlyList<PlatformCommissionRateDto>>
{
    private readonly IPlatformCommissionRepository _repository;

    public GetPlatformCommissionRatesHandler(IPlatformCommissionRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<PlatformCommissionRateDto>> Handle(GetPlatformCommissionRatesQuery request, CancellationToken cancellationToken)
    {
        var commissions = await _repository.GetByPlatformAsync(
            request.TenantId, request.Platform, request.IsActive, cancellationToken);

        return commissions.Select(c => new PlatformCommissionRateDto
        {
            Id = c.Id,
            Platform = c.Platform.ToString(),
            CommissionType = c.Type.ToString(),
            CategoryName = c.CategoryName,
            PlatformCategoryId = c.PlatformCategoryId,
            Rate = c.Rate,
            MinAmount = c.MinAmount,
            MaxAmount = c.MaxAmount,
            Currency = c.Currency,
            EffectiveFrom = c.EffectiveFrom,
            EffectiveTo = c.EffectiveTo,
            IsActive = c.IsActive,
            Notes = c.Notes
        }).ToList().AsReadOnly();
    }
}
