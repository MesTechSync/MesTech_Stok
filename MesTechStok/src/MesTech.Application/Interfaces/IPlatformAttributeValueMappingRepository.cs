using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces;

public interface IPlatformAttributeValueMappingRepository
{
    Task<PlatformAttributeValueMapping?> GetByInternalValueAsync(Guid tenantId, string attributeName, string value, PlatformType platform, CancellationToken ct = default);
    Task<IReadOnlyList<PlatformAttributeValueMapping>> GetByPlatformAsync(Guid tenantId, PlatformType platform, CancellationToken ct = default);
    Task AddAsync(PlatformAttributeValueMapping mapping, CancellationToken ct = default);
    Task UpdateAsync(PlatformAttributeValueMapping mapping, CancellationToken ct = default);
}
