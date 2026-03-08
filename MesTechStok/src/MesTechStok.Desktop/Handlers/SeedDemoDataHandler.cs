using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Commands.SeedDemoData;
using MesTechStok.Core.Data;

namespace MesTechStok.Desktop.Handlers;

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
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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
