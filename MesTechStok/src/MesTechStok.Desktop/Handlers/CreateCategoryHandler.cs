using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Commands.CreateCategory;
using MesTech.Domain.Entities;
using InfraDbContext = MesTech.Infrastructure.Persistence.AppDbContext;

namespace MesTechStok.Desktop.Handlers;

/// <summary>
/// H32: Migrated to Infrastructure.Persistence.AppDbContext.
/// </summary>
public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, CategoryCommandResult>
{
    private readonly IServiceProvider _serviceProvider;

    public CreateCategoryHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<CategoryCommandResult> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InfraDbContext>();

        if (await db.Categories.AnyAsync(c => c.Name == request.Name || c.Code == request.Code, cancellationToken))
        {
            return new CategoryCommandResult
            {
                IsSuccess = false,
                ErrorMessage = "Aynı ad veya kodda kategori mevcut.",
            };
        }

        var category = new Category
        {
            Name = request.Name,
            Code = request.Code,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return new CategoryCommandResult
        {
            IsSuccess = true,
            CategoryId = category.Id,
        };
    }
}
