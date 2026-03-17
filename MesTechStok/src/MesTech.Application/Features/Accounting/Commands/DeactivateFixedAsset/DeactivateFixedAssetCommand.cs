using MediatR;

namespace MesTech.Application.Features.Accounting.Commands.DeactivateFixedAsset;

/// <summary>
/// Sabit kiymet pasife alma komutu — hurda, satis, devir.
/// </summary>
/// <param name="Id">Sabit kiymet kimligi.</param>
/// <param name="TenantId">Kiraci kimligi.</param>
public record DeactivateFixedAssetCommand(
    Guid Id,
    Guid TenantId
) : IRequest<Unit>;
