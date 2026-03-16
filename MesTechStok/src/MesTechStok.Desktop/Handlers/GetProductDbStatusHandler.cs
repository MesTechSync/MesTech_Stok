using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MesTech.Application.Queries.GetProductDbStatus;
using InfraDbContext = MesTech.Infrastructure.Persistence.AppDbContext;

namespace MesTechStok.Desktop.Handlers;

/// <summary>
/// H32: Migrated to Infrastructure.Persistence.AppDbContext.
/// </summary>
public class GetProductDbStatusHandler : IRequestHandler<GetProductDbStatusQuery, ProductDbStatusDto>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GetProductDbStatusHandler>? _logger;

    public GetProductDbStatusHandler(IServiceProvider serviceProvider, ILogger<GetProductDbStatusHandler>? logger = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<ProductDbStatusDto> Handle(GetProductDbStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<InfraDbContext>();

            var canConnect = await ctx.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return new ProductDbStatusDto { IsConnected = false };
            }

            var aktif = await ctx.Products.AsNoTracking().CountAsync(p => p.IsActive, cancellationToken);
            var toplam = await ctx.Products.AsNoTracking().CountAsync(cancellationToken);

            return new ProductDbStatusDto
            {
                IsConnected = true,
                ActiveCount = aktif,
                TotalCount = toplam,
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "{ClassName} - {Context}", nameof(GetProductDbStatusHandler), "Database status check failed");
            return new ProductDbStatusDto { IsConnected = false };
        }
    }
}
