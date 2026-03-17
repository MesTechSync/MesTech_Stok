using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.CalculateDepreciation;

/// <summary>
/// Amortisman hesaplama sorgusu — belirli bir sabit kiymet icin tam tablo hesaplar.
/// VUK md. 315: Normal ve Azalan Bakiyeler yontemleri.
/// </summary>
/// <param name="AssetId">Sabit kiymet kimligi.</param>
public record CalculateDepreciationQuery(Guid AssetId)
    : IRequest<DepreciationResultDto>;
