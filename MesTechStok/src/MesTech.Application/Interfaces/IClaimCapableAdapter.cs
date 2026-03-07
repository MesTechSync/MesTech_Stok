using MesTech.Application.DTOs;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Iade yonetimi destekleyen platform adaptörleri icin interface.
/// </summary>
public interface IClaimCapableAdapter
{
    Task<IReadOnlyList<ExternalClaimDto>> PullClaimsAsync(DateTime? since = null, CancellationToken ct = default);
    Task<bool> ApproveClaimAsync(string claimId, CancellationToken ct = default);
    Task<bool> RejectClaimAsync(string claimId, string reason, CancellationToken ct = default);
}
