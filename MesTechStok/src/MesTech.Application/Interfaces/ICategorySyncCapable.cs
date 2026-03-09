using MesTech.Application.DTOs.Platform;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Kategori senkronizasyonu destekleyen platform adaptörleri icin interface.
/// Bidirectional: Pull tree (platform → local) ve Push (local → platform).
/// </summary>
public interface ICategorySyncCapable
{
    Task<IReadOnlyList<CategoryTreeSyncDto>> PullCategoryTreeAsync(CancellationToken ct = default);
    Task<bool> PushCategoryAsync(CategorySyncDto category, CancellationToken ct = default);
}
