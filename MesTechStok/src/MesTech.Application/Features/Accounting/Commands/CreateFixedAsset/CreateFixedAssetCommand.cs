using MediatR;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Features.Accounting.Commands.CreateFixedAsset;

/// <summary>
/// Yeni sabit kiymet olusturma komutu — VUK md. 313.
/// </summary>
/// <param name="TenantId">Kiraci kimligi.</param>
/// <param name="Name">Varlik adi.</param>
/// <param name="AssetCode">VUK hesap kodu (253, 254, 255 vb.).</param>
/// <param name="AcquisitionCost">Satin alma maliyeti (KDV haric).</param>
/// <param name="AcquisitionDate">Aktife alinma tarihi.</param>
/// <param name="UsefulLifeYears">Faydali omur (yil).</param>
/// <param name="Method">Amortisman yontemi.</param>
/// <param name="Description">Aciklama (opsiyonel).</param>
public record CreateFixedAssetCommand(
    Guid TenantId,
    string Name,
    string AssetCode,
    decimal AcquisitionCost,
    DateTime AcquisitionDate,
    int UsefulLifeYears,
    DepreciationMethod Method,
    string? Description = null
) : IRequest<Guid>;
