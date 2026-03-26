using MediatR;
using MesTech.Application.Commands.CreateCategory;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.DeleteCategory;

public sealed class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, CategoryCommandResult>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryHandler(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CategoryCommandResult> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = await _categoryRepository.GetByIdAsync(request.Id).ConfigureAwait(false);
        if (category == null)
            return new CategoryCommandResult { IsSuccess = false, ErrorMessage = $"Category {request.Id} not found." };

        await _categoryRepository.DeleteAsync(request.Id).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CategoryCommandResult
        {
            IsSuccess = true,
            CategoryId = request.Id,
        };
    }
}
