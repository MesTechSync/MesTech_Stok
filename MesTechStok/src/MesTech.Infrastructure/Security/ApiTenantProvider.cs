using MesTech.Domain.Interfaces;
using Microsoft.AspNetCore.Http;

namespace MesTech.Infrastructure.Security;

/// <summary>
/// Web API ortamı için ITenantProvider implementasyonu.
/// JWT "tenant_id" claim'inden TenantId okur.
/// Scoped lifetime — her HTTP request için yeni instance.
/// </summary>
public class ApiTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _tenantId;

    public ApiTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCurrentTenantId()
    {
        if (_tenantId.HasValue)
            return _tenantId.Value;

        var claim = _httpContextAccessor.HttpContext?
            .User.FindFirst("tenant_id")?.Value;

        if (Guid.TryParse(claim, out var id))
        {
            _tenantId = id;
            return id;
        }

        // Fallback: check X-Tenant-Id header (API key auth flow — no JWT)
        var headerValue = _httpContextAccessor.HttpContext?
            .Request.Headers["X-Tenant-Id"].FirstOrDefault();

        if (Guid.TryParse(headerValue, out var headerId))
        {
            _tenantId = headerId;
            return headerId;
        }

        throw new InvalidOperationException(
            "TenantId could not be resolved: no 'tenant_id' JWT claim or 'X-Tenant-Id' header found.");
    }

    /// <summary>
    /// Manually override tenant ID (for background jobs, tests, etc.).
    /// </summary>
    public void SetTenantId(Guid tenantId) => _tenantId = tenantId;
}
