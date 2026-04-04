using FluentAssertions;
using MesTech.Application.Commands.UpdateCategory;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: UpdateCategoryHandler testi — kategori güncelleme.
/// P1: Yanlış kategori eşleşmesi = ürün listeleme hatası.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class UpdateCategoryHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateCategoryHandler CreateSut() => new(_categoryRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_CategoryNotFound_ShouldReturnFailure()
    {
        _categoryRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Category?)null);
        var cmd = new UpdateCategoryCommand(Guid.NewGuid(), "Test", "TST", true);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldUpdateAndSave()
    {
        var category = new Category { Name = "Old", Code = "OLD", IsActive = false, TenantId = Guid.NewGuid() };
        _categoryRepo.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>())).ReturnsAsync(category);

        var cmd = new UpdateCategoryCommand(category.Id, "Elektronik", "ELK", true);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        category.Name.Should().Be("Elektronik");
        category.Code.Should().Be("ELK");
        category.IsActive.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
