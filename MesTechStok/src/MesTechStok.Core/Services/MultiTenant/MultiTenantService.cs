using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Security.Claims;
// Remove ASP.NET Core dependency in Core library; define minimal shims
using HttpContext = MesTechStok.Core.Services.MultiTenant.Http.HttpContextShim;
using RequestDelegate = System.Func<MesTechStok.Core.Services.MultiTenant.Http.HttpContextShim, System.Threading.Tasks.Task>;

namespace MesTechStok.Core.Services.MultiTenant
{
    /// <summary>
    /// Tenant bilgileri modeli
    /// </summary>
    public class TenantInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string DatabaseConnectionString { get; set; } = string.Empty;
        public string? DatabaseProvider { get; set; } = "PostgreSQL";
        public Dictionary<string, string> Settings { get; set; } = new();
        public Dictionary<string, object> Features { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastAccessedAt { get; set; }
        public TenantSubscriptionInfo Subscription { get; set; } = new();
        public TenantLimits Limits { get; set; } = new();
    }

    /// <summary>
    /// Tenant subscription bilgileri
    /// </summary>
    public class TenantSubscriptionInfo
    {
        public string PlanType { get; set; } = "Basic"; // Basic, Pro, Enterprise
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object> PlanFeatures { get; set; } = new();
    }

    /// <summary>
    /// Tenant limitleri
    /// </summary>
    public class TenantLimits
    {
        public int MaxUsers { get; set; } = 10;
        public long MaxStorageBytes { get; set; } = 1024 * 1024 * 1024; // 1GB
        public int MaxApiCallsPerHour { get; set; } = 1000;
        public int MaxConcurrentConnections { get; set; } = 50;
        public Dictionary<string, object> CustomLimits { get; set; } = new();
    }

    /// <summary>
    /// Tenant context - current işlem için tenant bilgilerini tutar
    /// </summary>
    public class TenantContext
    {
        public TenantInfo? CurrentTenant { get; set; }
        public ClaimsPrincipal? User { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Properties { get; set; } = new();

        public bool HasTenant => CurrentTenant != null;
        public string TenantId => CurrentTenant?.Id ?? string.Empty;
        public string TenantName => CurrentTenant?.Name ?? string.Empty;
    }

    /// <summary>
    /// Multi-tenant ayarları
    /// </summary>
    public class MultiTenantSettings
    {
        public TenantResolutionStrategy ResolutionStrategy { get; set; } = TenantResolutionStrategy.Header;
        public string TenantHeaderName { get; set; } = "X-Tenant-Id";
        public string TenantClaimType { get; set; } = "tenant_id";
        public string DefaultTenantId { get; set; } = "default";
        public bool EnableTenantValidation { get; set; } = true;
        public bool EnableDataIsolation { get; set; } = true;
        public bool EnableFeatureToggling { get; set; } = true;
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(15);
    }

    /// <summary>
    /// Tenant resolution stratejileri
    /// </summary>
    public enum TenantResolutionStrategy
    {
        Header,       // HTTP header'dan
        Subdomain,    // Subdomain'den (api.tenant1.example.com)
        Path,         // URL path'den (/tenant1/api/...)
        Claim,        // JWT claim'den
        QueryString,  // Query string'den (?tenant=tenant1)
        Cookie        // Cookie'den
    }

    /// <summary>
    /// Tenant store interface
    /// </summary>
    public interface ITenantStore
    {
        Task<TenantInfo?> GetTenantAsync(string tenantId);
        Task<IEnumerable<TenantInfo>> GetAllTenantsAsync();
        Task<TenantInfo> CreateTenantAsync(TenantInfo tenantInfo);
        Task<bool> UpdateTenantAsync(TenantInfo tenantInfo);
        Task<bool> DeleteTenantAsync(string tenantId);
        Task<bool> TenantExistsAsync(string tenantId);
        Task UpdateLastAccessAsync(string tenantId);
    }

    /// <summary>
    /// Tenant resolver interface
    /// </summary>
    public interface ITenantResolver
    {
        Task<string?> ResolveTenantIdAsync(HttpContext context);
        Task<TenantInfo?> ResolveTenantAsync(HttpContext context);
    }

    /// <summary>
    /// Tenant context accessor
    /// </summary>
    public interface ITenantContextAccessor
    {
        TenantContext? Current { get; set; }
    }

    /// <summary>
    /// Multi-tenant service interface
    /// </summary>
    public interface IMultiTenantService
    {
        Task<TenantInfo?> GetCurrentTenantAsync();
        Task<TenantInfo?> GetTenantAsync(string tenantId);
        Task<bool> ValidateTenantAccessAsync(string tenantId, ClaimsPrincipal? user = null);
        Task<bool> CheckFeatureAsync(string featureName);
        Task<T?> GetTenantSettingAsync<T>(string settingName, T? defaultValue = default);
        Task SetTenantSettingAsync<T>(string settingName, T value);
        Task<bool> CheckLimitAsync(string limitName, int currentValue);
        Task TrackUsageAsync(string metricName, double value = 1);
    }

    /// <summary>
    /// Tenant context accessor implementasyonu
    /// </summary>
    public class TenantContextAccessor : ITenantContextAccessor
    {
        private readonly AsyncLocal<TenantContext?> _current = new();

        public TenantContext? Current
        {
            get => _current.Value;
            set => _current.Value = value;
        }
    }

    /// <summary>
    /// In-memory tenant store (development/testing için)
    /// </summary>
    public class InMemoryTenantStore : ITenantStore
    {
        private readonly Dictionary<string, TenantInfo> _tenants = new();
        private readonly ILogger<InMemoryTenantStore> _logger;

        public InMemoryTenantStore(ILogger<InMemoryTenantStore> logger)
        {
            _logger = logger;

            // Default tenant oluştur
            var defaultTenant = new TenantInfo
            {
                Id = "default",
                Name = "default",
                DisplayName = "Default Tenant",
                DatabaseConnectionString = "DefaultConnection",
                Subscription = new TenantSubscriptionInfo
                {
                    PlanType = "Enterprise",
                    PlanFeatures = new Dictionary<string, object>
                    {
                        { "unlimited_users", true },
                        { "api_access", true },
                        { "custom_features", true }
                    }
                },
                Limits = new TenantLimits
                {
                    MaxUsers = 1000,
                    MaxApiCallsPerHour = 10000,
                    MaxConcurrentConnections = 500
                }
            };

            _tenants[defaultTenant.Id] = defaultTenant;
        }

        public async Task<TenantInfo?> GetTenantAsync(string tenantId)
        {
            await Task.CompletedTask;
            return _tenants.TryGetValue(tenantId, out var tenant) ? tenant : null;
        }

        public async Task<IEnumerable<TenantInfo>> GetAllTenantsAsync()
        {
            await Task.CompletedTask;
            return _tenants.Values.Where(t => t.IsActive);
        }

        public async Task<TenantInfo> CreateTenantAsync(TenantInfo tenantInfo)
        {
            await Task.CompletedTask;

            if (_tenants.ContainsKey(tenantInfo.Id))
            {
                throw new InvalidOperationException($"Tenant with ID '{tenantInfo.Id}' already exists");
            }

            tenantInfo.CreatedAt = DateTime.UtcNow;
            _tenants[tenantInfo.Id] = tenantInfo;

            _logger.LogInformation("[TenantStore] Created tenant: {TenantId} - {TenantName}",
                tenantInfo.Id, tenantInfo.DisplayName);

            return tenantInfo;
        }

        public async Task<bool> UpdateTenantAsync(TenantInfo tenantInfo)
        {
            await Task.CompletedTask;

            if (!_tenants.ContainsKey(tenantInfo.Id))
            {
                return false;
            }

            _tenants[tenantInfo.Id] = tenantInfo;

            _logger.LogInformation("[TenantStore] Updated tenant: {TenantId}", tenantInfo.Id);
            return true;
        }

        public async Task<bool> DeleteTenantAsync(string tenantId)
        {
            await Task.CompletedTask;

            if (_tenants.Remove(tenantId))
            {
                _logger.LogInformation("[TenantStore] Deleted tenant: {TenantId}", tenantId);
                return true;
            }

            return false;
        }

        public async Task<bool> TenantExistsAsync(string tenantId)
        {
            await Task.CompletedTask;
            return _tenants.ContainsKey(tenantId);
        }

        public async Task UpdateLastAccessAsync(string tenantId)
        {
            await Task.CompletedTask;

            if (_tenants.TryGetValue(tenantId, out var tenant))
            {
                tenant.LastAccessedAt = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Tenant resolver implementasyonu
    /// </summary>
    public class TenantResolver : ITenantResolver
    {
        private readonly ITenantStore _tenantStore;
        private readonly ILogger<TenantResolver> _logger;
        private readonly MultiTenantSettings _settings;

        public TenantResolver(ITenantStore tenantStore, ILogger<TenantResolver> logger, IOptions<MultiTenantSettings> settings)
        {
            _tenantStore = tenantStore;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<string?> ResolveTenantIdAsync(HttpContext context)
        {
            string? tenantId = null;

            try
            {
                tenantId = _settings.ResolutionStrategy switch
                {
                    TenantResolutionStrategy.Header => ResolveFromHeader(context),
                    TenantResolutionStrategy.Subdomain => ResolveFromSubdomain(context),
                    TenantResolutionStrategy.Path => ResolveFromPath(context),
                    TenantResolutionStrategy.Claim => ResolveFromClaim(context),
                    TenantResolutionStrategy.QueryString => ResolveFromQueryString(context),
                    TenantResolutionStrategy.Cookie => ResolveFromCookie(context),
                    _ => null
                };

                // Default tenant fallback
                if (string.IsNullOrEmpty(tenantId))
                {
                    tenantId = _settings.DefaultTenantId;
                    _logger.LogDebug("[TenantResolver] Using default tenant: {TenantId}", tenantId);
                }

                return tenantId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TenantResolver] Error resolving tenant ID");
                return _settings.DefaultTenantId;
            }
        }

        public async Task<TenantInfo?> ResolveTenantAsync(HttpContext context)
        {
            var tenantId = await ResolveTenantIdAsync(context);

            if (string.IsNullOrEmpty(tenantId))
            {
                return null;
            }

            var tenant = await _tenantStore.GetTenantAsync(tenantId);

            if (tenant == null)
            {
                _logger.LogWarning("[TenantResolver] Tenant not found: {TenantId}", tenantId);
                return null;
            }

            if (!tenant.IsActive)
            {
                _logger.LogWarning("[TenantResolver] Tenant is inactive: {TenantId}", tenantId);
                return null;
            }

            // Last access update
            await _tenantStore.UpdateLastAccessAsync(tenantId);

            _logger.LogDebug("[TenantResolver] Resolved tenant: {TenantId} - {TenantName}",
                tenant.Id, tenant.DisplayName);

            return tenant;
        }

        private string? ResolveFromHeader(HttpContext context)
        {
            return context.Request.Headers.TryGetValue(_settings.TenantHeaderName, out var val) ? val : null;
        }

        private string? ResolveFromSubdomain(HttpContext context)
        {
            var host = context.Request.Host.Host;
            var parts = host.Split('.');

            return parts.Length > 2 ? parts[0] : null; // api.tenant1.example.com -> tenant1
        }

        private string? ResolveFromPath(HttpContext context)
        {
            var path = context.Request.Path.Value;
            if (string.IsNullOrEmpty(path))
                return null;

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return segments.Length > 0 ? segments[0] : null; // /tenant1/api/... -> tenant1
        }

        private string? ResolveFromClaim(HttpContext context)
        {
            return context.User?.FindFirst(_settings.TenantClaimType)?.Value;
        }

        private string? ResolveFromQueryString(HttpContext context)
        {
            return context.Request.Query.TryGetValue("tenant", out var q) ? q : null;
        }

        private string? ResolveFromCookie(HttpContext context)
        {
            return context.Request.Cookies.TryGetValue("tenant", out var c) ? c : null;
        }
    }

    /// <summary>
    /// Multi-tenant service implementasyonu
    /// </summary>
    public class MultiTenantService : IMultiTenantService
    {
        private readonly ITenantContextAccessor _contextAccessor;
        private readonly ITenantStore _tenantStore;
        private readonly ILogger<MultiTenantService> _logger;

        public MultiTenantService(
            ITenantContextAccessor contextAccessor,
            ITenantStore tenantStore,
            ILogger<MultiTenantService> logger)
        {
            _contextAccessor = contextAccessor;
            _tenantStore = tenantStore;
            _logger = logger;
        }

        public async Task<TenantInfo?> GetCurrentTenantAsync()
        {
            return _contextAccessor.Current?.CurrentTenant;
        }

        public async Task<TenantInfo?> GetTenantAsync(string tenantId)
        {
            return await _tenantStore.GetTenantAsync(tenantId);
        }

        public async Task<bool> ValidateTenantAccessAsync(string tenantId, ClaimsPrincipal? user = null)
        {
            var tenant = await _tenantStore.GetTenantAsync(tenantId);

            if (tenant == null || !tenant.IsActive)
            {
                return false;
            }

            // Subscription kontrolü
            if (!tenant.Subscription.IsActive)
            {
                _logger.LogWarning("[MultiTenant] Tenant subscription is inactive: {TenantId}", tenantId);
                return false;
            }

            // Subscription süresi kontrolü
            if (tenant.Subscription.EndDate.HasValue &&
                DateTime.UtcNow > tenant.Subscription.EndDate.Value)
            {
                _logger.LogWarning("[MultiTenant] Tenant subscription expired: {TenantId}, ExpiredAt: {ExpiredAt}",
                    tenantId, tenant.Subscription.EndDate.Value);
                return false;
            }

            // TODO: User-tenant ilişkisi kontrolü
            // if (user != null)
            // {
            //     var userTenants = GetUserTenants(user);
            //     if (!userTenants.Contains(tenantId))
            //     {
            //         return false;
            //     }
            // }

            return true;
        }

        public async Task<bool> CheckFeatureAsync(string featureName)
        {
            var tenant = await GetCurrentTenantAsync();

            if (tenant == null)
            {
                return false;
            }

            // Tenant-specific feature kontrolü
            if (tenant.Features.TryGetValue(featureName, out var featureValue))
            {
                return Convert.ToBoolean(featureValue);
            }

            // Subscription plan feature kontrolü
            if (tenant.Subscription.PlanFeatures.TryGetValue(featureName, out var planFeatureValue))
            {
                return Convert.ToBoolean(planFeatureValue);
            }

            return false;
        }

        public async Task<T?> GetTenantSettingAsync<T>(string settingName, T? defaultValue = default)
        {
            var tenant = await GetCurrentTenantAsync();

            if (tenant?.Settings.TryGetValue(settingName, out var settingValue) == true)
            {
                try
                {
                    if (typeof(T) == typeof(string))
                    {
                        return (T)(object)settingValue;
                    }

                    return JsonSerializer.Deserialize<T>(settingValue);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[MultiTenant] Failed to deserialize setting {SettingName} for tenant {TenantId}",
                        settingName, tenant.Id);
                }
            }

            return defaultValue;
        }

        public async Task SetTenantSettingAsync<T>(string settingName, T value)
        {
            var tenant = await GetCurrentTenantAsync();

            if (tenant == null)
            {
                throw new InvalidOperationException("No current tenant context");
            }

            var settingValue = typeof(T) == typeof(string)
                ? value?.ToString() ?? string.Empty
                : JsonSerializer.Serialize(value);

            tenant.Settings[settingName] = settingValue;

            await _tenantStore.UpdateTenantAsync(tenant);

            _logger.LogDebug("[MultiTenant] Updated setting {SettingName} for tenant {TenantId}",
                settingName, tenant.Id);
        }

        public async Task<bool> CheckLimitAsync(string limitName, int currentValue)
        {
            var tenant = await GetCurrentTenantAsync();

            if (tenant == null)
            {
                return false;
            }

            var limit = limitName switch
            {
                "users" => tenant.Limits.MaxUsers,
                "api_calls" => tenant.Limits.MaxApiCallsPerHour,
                "connections" => tenant.Limits.MaxConcurrentConnections,
                _ => GetCustomLimit(tenant, limitName)
            };

            return currentValue <= limit;
        }

        public async Task TrackUsageAsync(string metricName, double value = 1)
        {
            var tenant = await GetCurrentTenantAsync();

            if (tenant == null)
            {
                return;
            }

            // TODO: Usage tracking implementasyonu
            // Metrics service'e gönder

            _logger.LogDebug("[MultiTenant] Tracked usage {MetricName}={Value} for tenant {TenantId}",
                metricName, value, tenant.Id);
        }

        private int GetCustomLimit(TenantInfo tenant, string limitName)
        {
            if (tenant.Limits.CustomLimits.TryGetValue(limitName, out var limitValue))
            {
                return Convert.ToInt32(limitValue);
            }

            return int.MaxValue; // Unlimited
        }
    }

    /// <summary>
    /// Multi-tenant middleware
    /// HTTP request'lerde tenant context oluşturur
    /// </summary>
    public class MultiTenantMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ITenantResolver _tenantResolver;
        private readonly ITenantContextAccessor _contextAccessor;
        private readonly ILogger<MultiTenantMiddleware> _logger;
        private readonly MultiTenantSettings _settings;

        public MultiTenantMiddleware(
            RequestDelegate next,
            ITenantResolver tenantResolver,
            ITenantContextAccessor contextAccessor,
            ILogger<MultiTenantMiddleware> logger,
            IOptions<MultiTenantSettings> settings)
        {
            _next = next;
            _tenantResolver = tenantResolver;
            _contextAccessor = contextAccessor;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();

            try
            {
                // Tenant resolve
                var tenant = await _tenantResolver.ResolveTenantAsync(context);

                if (tenant == null && _settings.EnableTenantValidation)
                {
                    _logger.LogWarning("[MultiTenant] No valid tenant found for request: {Path}",
                        context.Request.Path);

                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Invalid or missing tenant");
                    return;
                }

                // Tenant context oluştur
                var tenantContext = new TenantContext
                {
                    CurrentTenant = tenant,
                    User = context.User,
                    CorrelationId = MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId,
                    Properties = new Dictionary<string, object>
                    {
                        { "RequestPath", context.Request.Path.Value ?? string.Empty },
                        { "RequestMethod", context.Request.Method },
                        { "UserAgent", context.Request.Headers["User-Agent"].ToString() }
                    }
                };

                _contextAccessor.Current = tenantContext;

                if (tenant != null)
                {
                    _logger.LogDebug("[MultiTenant] Set tenant context: {TenantId} for request: {Path}",
                        tenant.Id, context.Request.Path);
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MultiTenant] Error in multi-tenant middleware");
                throw;
            }
            finally
            {
                _contextAccessor.Current = null;
            }
        }
    }
}
