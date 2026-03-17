using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Services;

namespace MesTech.Application.Features.Accounting.Queries.CalculateDepreciation;

/// <summary>
/// Amortisman hesaplama handler.
/// FixedAsset entity'sini yukler, DepreciationCalculationService ile tam tablo olusturur.
/// </summary>
public class CalculateDepreciationHandler
    : IRequestHandler<CalculateDepreciationQuery, DepreciationResultDto>
{
    private readonly IFixedAssetRepository _assetRepo;
    private readonly DepreciationCalculationService _depreciationService;

    public CalculateDepreciationHandler(
        IFixedAssetRepository assetRepo,
        DepreciationCalculationService depreciationService)
    {
        _assetRepo = assetRepo;
        _depreciationService = depreciationService;
    }

    public async Task<DepreciationResultDto> Handle(
        CalculateDepreciationQuery request,
        CancellationToken cancellationToken)
    {
        var asset = await _assetRepo.GetByIdAsync(request.AssetId, cancellationToken)
            ?? throw new KeyNotFoundException($"Sabit kiymet bulunamadi: {request.AssetId}");

        var currentYear = _depreciationService.CalculateAnnual(asset);
        var schedule = _depreciationService.GenerateSchedule(asset);

        return new DepreciationResultDto
        {
            AssetId = asset.Id,
            AssetName = asset.Name,
            AcquisitionCost = asset.AcquisitionCost,
            Method = asset.Method.ToString(),
            UsefulLifeYears = asset.UsefulLifeYears,
            CurrentYearDepreciation = currentYear,
            Schedule = schedule.Select(s => new DepreciationScheduleLineDto
            {
                Year = s.Year,
                DepreciationAmount = s.DepreciationAmount,
                AccumulatedDepreciation = s.AccumulatedDepreciation,
                NetBookValue = s.NetBookValue
            }).ToList()
        };
    }
}
