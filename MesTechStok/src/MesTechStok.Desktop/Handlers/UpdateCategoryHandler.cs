using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Commands.CreateCategory;
using MesTech.Application.Commands.UpdateCategory;
using MesTechStok.Core.Data;

namespace MesTechStok.Desktop.Handlers;

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, CategoryCommandResult>
{
    private readonly IServiceProvider _serviceProvider;

    public UpdateCategoryHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<CategoryCommandResult> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (cat == null)
        {
            return new CategoryCommandResult
            {
                IsSuccess = false,
                ErrorMessage = "Kategori bulunamadı.",
            };
        }

        var code = string.IsNullOrWhiteSpace(request.Code)
            ? request.Name.ToUpperInvariant().Replace(' ', '_')
            : request.Code;

        if (await db.Categories.AnyAsync(
                c => (c.Name == request.Name || c.Code == code) && c.Id != request.Id,
                cancellationToken))
        {
            return new CategoryCommandResult
            {
                IsSuccess = false,
                ErrorMessage = "Aynı ad/kod mevcut.",
            };
        }

        cat.Name = request.Name;
        cat.Code = code;
        cat.IsActive = request.IsActive;
        cat.ModifiedDate = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return new CategoryCommandResult
        {
            IsSuccess = true,
            CategoryId = cat.Id,
        };
    }
}
