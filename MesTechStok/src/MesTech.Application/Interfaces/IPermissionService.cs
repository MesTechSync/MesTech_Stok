namespace MesTech.Application.Interfaces;

/// <summary>
/// OWASP ASVS V4 Access Control — permission-based yetkilendirme.
/// Role→Permission zinciri uzerinden kullanicinin belirli isleme yetkisi var mi kontrol eder.
/// </summary>
public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, string permissionName, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetUserPermissionsAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsInRoleAsync(Guid userId, string roleName, CancellationToken ct = default);
}
