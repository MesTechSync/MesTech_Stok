using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Commands.CreateBulkProducts;
using MesTech.Domain.Entities;
using InfraDbContext = MesTech.Infrastructure.Persistence.AppDbContext;

namespace MesTechStok.Desktop.Handlers;

/// <summary>
/// H32: Migrated to Infrastructure.Persistence.AppDbContext.
/// </summary>
public class CreateBulkProductsHandler : IRequestHandler<CreateBulkProductsCommand, CreateBulkProductsResult>
{
    private readonly IServiceProvider _serviceProvider;

    public CreateBulkProductsHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<CreateBulkProductsResult> Handle(CreateBulkProductsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<InfraDbContext>();

            var rand = new Random();
            var catId = await ctx.Categories.Select(c => c.Id).FirstOrDefaultAsync(cancellationToken);
            if (catId == Guid.Empty)
            {
                ctx.Categories.Add(new Category
                {
                    Name = "Genel",
                    Code = "GENEL",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                });
                await ctx.SaveChangesAsync(cancellationToken);
                catId = await ctx.Categories.Select(c => c.Id).FirstOrDefaultAsync(cancellationToken);
            }

            int created = 0;
            for (int i = 0; i < request.Count; i++)
            {
                var barcode = (8690000000000L + rand.Next(1, 900000)).ToString();
                if (await ctx.Products.AnyAsync(p => p.Barcode == barcode, cancellationToken))
                    continue;

                ctx.Products.Add(new Product
                {
                    Name = $"Hızlı Ürün {DateTime.Now:HHmmss}-{i + 1}",
                    SKU = $"FAST-{Guid.NewGuid():N}"[..16],
                    Barcode = barcode,
                    CategoryId = catId,
                    PurchasePrice = rand.Next(50, 500),
                    SalePrice = rand.Next(60, 900),
                    Stock = rand.Next(0, 120),
                    MinimumStock = 5,
                    TaxRate = 18m,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                });
                created++;
            }

            await ctx.SaveChangesAsync(cancellationToken);

            return new CreateBulkProductsResult
            {
                IsSuccess = true,
                CreatedCount = created,
                Message = $"{created} ürün oluşturuldu.",
            };
        }
        catch (Exception ex)
        {
            return new CreateBulkProductsResult
            {
                IsSuccess = false,
                Message = ex.Message,
            };
        }
    }
}
