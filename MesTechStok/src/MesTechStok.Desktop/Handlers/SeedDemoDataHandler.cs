using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Commands.SeedDemoData;
#pragma warning disable CS0618 // Obsolete legacy AppDbContext — SeedDemoDataAsync() lives on Core context, requires extraction to ISeedService (TODO H33)
using MesTechStok.Core.Data;
#pragma warning restore CS0618

namespace MesTechStok.Desktop.Handlers;

/// <summary>
/// TODO H33: Extract SeedDemoDataAsync() from legacy AppDbContext into an ISeedService,
///   then inject Infrastructure.Persistence.AppDbContext here.
///   Blocked: SeedDemoDataAsync() is a 200+ line method on legacy AppDbContext with Core.Data.Models references.
/// </summary>
public class SeedDemoDataHandler : IRequestHandler<SeedDemoDataCommand, SeedDemoDataResult>
{
    private readonly IServiceProvider _serviceProvider;

    public SeedDemoDataHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<SeedDemoDataResult> Handle(SeedDemoDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
#pragma warning disable CS0618 // TODO H33: Legacy AppDbContext still needed for SeedDemoDataAsync()
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
#pragma warning restore CS0618

            await ctx.Database.EnsureCreatedAsync(cancellationToken);

            var hasActive = await ctx.Products.AnyAsync(p => p.IsActive, cancellationToken);
            if (hasActive)
            {
                return new SeedDemoDataResult
                {
                    IsSuccess = true,
                    WasSkipped = true,
                    Message = "Aktif ürünler mevcut, demo yükleme atlandı.",
                };
            }

            await ctx.SeedDemoDataAsync();

            return new SeedDemoDataResult
            {
                IsSuccess = true,
                WasSkipped = false,
                Message = "Demo verileri yüklendi.",
            };
        }
        catch (Exception ex)
        {
            return new SeedDemoDataResult
            {
                IsSuccess = false,
                Message = ex.Message,
            };
        }
    }
}
