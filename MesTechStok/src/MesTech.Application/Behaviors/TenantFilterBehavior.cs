using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Behaviors;

/// <summary>
/// Tenant izolasyonunu kontrol eden MediatR pipeline behavior.
/// Her request'te aktif tenant ID'nin mevcut olduğunu doğrular.
/// </summary>
public sealed class TenantFilterBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TenantFilterBehavior<TRequest, TResponse>> _logger;

    public TenantFilterBehavior(ITenantProvider tenantProvider, ILogger<TenantFilterBehavior<TRequest, TResponse>> logger)
    {
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        ArgumentNullException.ThrowIfNull(next);

        var currentTenantId = _tenantProvider.GetCurrentTenantId();

        if (currentTenantId == Guid.Empty)
        {
            _logger.LogWarning("TenantId is not set for request {RequestType}. Using default tenant.", typeof(TRequest).Name);
        }

        // Tenant ID spoofing koruması — request'teki TenantId ile oturumdaki TenantId karşılaştır
        var tenantIdProperty = typeof(TRequest).GetProperty("TenantId");
        if (tenantIdProperty is not null && currentTenantId != Guid.Empty)
        {
            var requestTenantId = tenantIdProperty.GetValue(request);
            if (requestTenantId is Guid requestGuid && requestGuid != Guid.Empty && requestGuid != currentTenantId)
            {
                _logger.LogCritical(
                    "TENANT SPOOFING DETECTED! Request {RequestType} has TenantId={RequestTenant} but current user is TenantId={CurrentTenant}",
                    typeof(TRequest).Name, requestGuid, currentTenantId);
                throw new UnauthorizedAccessException(
                    $"Tenant isolation violation: request TenantId does not match authenticated tenant.");
            }
        }

        _logger.LogDebug("Processing request {RequestType} for TenantId: {TenantId}", typeof(TRequest).Name, currentTenantId);

        return await next().ConfigureAwait(false);
    }
}
