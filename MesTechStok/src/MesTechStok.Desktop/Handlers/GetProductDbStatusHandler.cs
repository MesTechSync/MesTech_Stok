using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Queries.GetProductDbStatus;
using InfraDbContext = MesTech.Infrastructure.Persistence.AppDbContext;

namespace MesTechStok.Desktop.Handlers;

/// <summary>
/// H32: Migrated from Core.AppDbContext to Infrastructure.Persistence.AppDbContext.
/// </summary>
public class GetProductDbStatusHandler : IRequestHandler<GetProductDbStatusQuery, ProductDbStatusDto>
{
    private readonly IServiceProvider _serviceProvider;

    public GetProductDbStatusHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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
        catch
        {
            return new ProductDbStatusDto { IsConnected = false };
        }
    }
}
