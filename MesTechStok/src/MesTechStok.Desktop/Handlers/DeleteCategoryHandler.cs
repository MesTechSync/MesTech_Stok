using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Commands.CreateCategory;
using MesTech.Application.Commands.DeleteCategory;
using InfraDbContext = MesTech.Infrastructure.Persistence.AppDbContext;

namespace MesTechStok.Desktop.Handlers;

/// <summary>
/// H32: Migrated from Core.AppDbContext to Infrastructure.Persistence.AppDbContext.
/// </summary>
public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, CategoryCommandResult>
{
    private readonly IServiceProvider _serviceProvider;

    public DeleteCategoryHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<CategoryCommandResult> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InfraDbContext>();

        var cat = await db.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (cat == null)
        {
            return new CategoryCommandResult
            {
                IsSuccess = false,
                ErrorMessage = "Kategori bulunamadı.",
            };
        }

        if (cat.Products.Any())
        {
            cat.IsActive = false;
            await db.SaveChangesAsync(cancellationToken);
        }
        else
        {
            db.Categories.Remove(cat);
            await db.SaveChangesAsync(cancellationToken);
        }

        return new CategoryCommandResult
        {
            IsSuccess = true,
            CategoryId = cat.Id,
        };
    }
}
