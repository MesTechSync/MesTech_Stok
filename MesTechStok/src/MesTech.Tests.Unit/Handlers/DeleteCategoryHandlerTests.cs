using FluentAssertions;
using MesTech.Application.Commands.DeleteCategory;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class DeleteCategoryHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeleteCategoryHandler _sut;

    public DeleteCategoryHandlerTests()
    {
        _sut = new DeleteCategoryHandler(_categoryRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingCategory_DeletesAndReturnsSuccess()
    {
        var categoryId = Guid.NewGuid();
        var category = new Category { Id = categoryId, Name = "Test", Code = "TST" };
        _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync(category);

        var cmd = new DeleteCategoryCommand(categoryId);
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.CategoryId.Should().Be(categoryId);
        _categoryRepoMock.Verify(r => r.DeleteAsync(categoryId), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentCategory_ReturnsFail()
    {
        var categoryId = Guid.NewGuid();
        _categoryRepoMock.Setup(r => r.GetByIdAsync(categoryId)).ReturnsAsync((Category?)null);

        var cmd = new DeleteCategoryCommand(categoryId);
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
        _categoryRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
