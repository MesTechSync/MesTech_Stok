using System.Threading.Tasks;

namespace MesTechStok.Desktop.Services
{
    /// <summary>
    /// Authorization service interface for role-based access control
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Check if current user has specific permission
        /// </summary>
        Task<bool> IsAuthorizedAsync(string permission);

        /// <summary>
        /// Check if current user has specific role
        /// </summary>
        Task<bool> HasRoleAsync(string role);

        /// <summary>
        /// Get current user permissions
        /// </summary>
        Task<string[]> GetUserPermissionsAsync();

        /// <summary>
        /// Check if user is admin
        /// </summary>
        Task<bool> IsAdminAsync();

        /// <summary>
        /// Check if user is allowed to perform specific action (alias for IsAuthorizedAsync)
        /// </summary>
        Task<bool> IsAllowedAsync(string action);

        /// <summary>
        /// Check if user is allowed to perform specific action on module
        /// </summary>
        Task<bool> IsAllowedAsync(string module, string permission);
    }
}
