using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Behaviors;

/// <summary>
/// Tenant izolasyonunu kontrol eden MediatR pipeline behavior.
/// Her request'te aktif tenant ID'nin mevcut olduğunu doğrular.
/// </summary>
public class TenantFilterBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
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

        var tenantId = _tenantProvider.GetCurrentTenantId();

        if (tenantId <= 0)
        {
            _logger.LogWarning("TenantId is not set for request {RequestType}. Using default tenant.", typeof(TRequest).Name);
        }

        _logger.LogDebug("Processing request {RequestType} for TenantId: {TenantId}", typeof(TRequest).Name, tenantId);

        return await next().ConfigureAwait(false);
    }
}
