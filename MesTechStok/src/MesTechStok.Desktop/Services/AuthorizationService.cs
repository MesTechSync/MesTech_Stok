using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MesTechStok.Desktop.Services
{
    /// <summary>
    /// Authorization service implementation
    /// </summary>
    public class AuthorizationService : IAuthorizationService
    {
        private readonly ILogger<AuthorizationService> _logger;
        private readonly SimpleSecurityService _securityService;

        public AuthorizationService(
            ILogger<AuthorizationService> logger,
            SimpleSecurityService securityService)
        {
            _logger = logger;
            _securityService = securityService;
        }

        public async Task<bool> IsAuthorizedAsync(string permission)
        {
            try
            {
                _logger.LogInformation($"Checking permission: {permission}");

                // Şimdilik her authenticated kullanıcıya full yetki (hızlı fix)
                var isAuthenticated = await _securityService.IsAuthenticatedAsync();
                if (!isAuthenticated)
                {
                    _logger.LogWarning("User not authenticated");
                    return false;
                }

                // TODO: Database'den gerçek permission kontrolü (gelecek geliştirme)
                _logger.LogInformation($"Permission '{permission}' granted");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking permission: {permission}");
                return false;
            }
        }

        public async Task<bool> HasRoleAsync(string role)
        {
            try
            {
                _logger.LogInformation($"Checking role: {role}");

                var isAuthenticated = await _securityService.IsAuthenticatedAsync();
                if (!isAuthenticated) return false;

                // TODO: Database'den gerçek role kontrolü (gelecek geliştirme)
                // Şimdilik authenticated = admin sayıyoruz
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking role: {role}");
                return false;
            }
        }

        public async Task<string[]> GetUserPermissionsAsync()
        {
            try
            {
                var isAuthenticated = await _securityService.IsAuthenticatedAsync();
                if (!isAuthenticated) return new string[0];

                // TODO: Database'den kullanıcı permissions'larını çek (gelecek geliştirme)
                return new[] { "READ", "WRITE", "DELETE", "ADMIN" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user permissions");
                return new string[0];
            }
        }

        public async Task<bool> IsAdminAsync()
        {
            return await HasRoleAsync("Admin");
        }

        public async Task<bool> IsAllowedAsync(string action)
        {
            // IsAllowedAsync is alias for IsAuthorizedAsync
            return await IsAuthorizedAsync(action);
        }

        public async Task<bool> IsAllowedAsync(string module, string permission)
        {
            try
            {
                _logger.LogInformation($"Checking module permission: {module}.{permission}");

                var isAuthenticated = await _securityService.IsAuthenticatedAsync();
                if (!isAuthenticated)
                {
                    _logger.LogWarning("User not authenticated for module permission check");
                    return false;
                }

                // TODO: Database'den gerçek module-based permission kontrolü (gelecek geliştirme)
                // Şimdilik authenticated users için all permissions allowed
                _logger.LogInformation($"Module permission '{module}.{permission}' granted");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking module permission: {module}.{permission}");
                return false;
            }
        }
    }
}
