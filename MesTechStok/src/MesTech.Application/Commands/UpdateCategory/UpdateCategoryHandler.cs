using MediatR;
using MesTech.Application.Commands.CreateCategory;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.UpdateCategory;

public sealed class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, CategoryCommandResult>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCategoryHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CategoryCommandResult> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (category == null)
            return new CategoryCommandResult { IsSuccess = false, ErrorMessage = $"Category {request.Id} not found." };

        category.Name = request.Name;
        category.Code = request.Code;
        category.IsActive = request.IsActive;

        await _categoryRepository.UpdateAsync(category, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CategoryCommandResult
        {
            IsSuccess = true,
            CategoryId = category.Id,
        };
    }
}
