using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.UpdateFixedAsset;

/// <summary>
/// Sabit kiymet guncelleme komutu.
/// AcquisitionCost degistirilemez (immutable) — yalnizca Name, Description, UsefulLifeYears.
/// </summary>
/// <param name="Id">Sabit kiymet kimligi.</param>
/// <param name="TenantId">Kiraci kimligi.</param>
/// <param name="Name">Yeni varlik adi.</param>
/// <param name="Description">Yeni aciklama.</param>
/// <param name="UsefulLifeYears">Yeni faydali omur (yil).</param>
public record UpdateFixedAssetCommand(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    int UsefulLifeYears
) : IRequest<Unit>;
