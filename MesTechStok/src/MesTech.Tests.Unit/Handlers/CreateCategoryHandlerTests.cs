using FluentAssertions;
using MesTech.Application.Commands.CreateCategory;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateCategoryHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateCategoryHandler _sut;

    public CreateCategoryHandlerTests()
    {
        _sut = new CreateCategoryHandler(_categoryRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesCategoryAndReturnsSuccess()
    {
        var cmd = new CreateCategoryCommand("Elektronik", "ELK");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.CategoryId.Should().NotBe(Guid.Empty);

        _categoryRepoMock.Verify(r => r.AddAsync(It.Is<Category>(c =>
            c.Name == "Elektronik" && c.Code == "ELK" && c.IsActive)), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InactiveCategory_SetsIsActiveFalse()
    {
        var cmd = new CreateCategoryCommand("Arşiv", "ARS", IsActive: false);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _categoryRepoMock.Verify(r => r.AddAsync(It.Is<Category>(c => !c.IsActive)), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullRepository_Throws()
    {
        var act = () => new CreateCategoryHandler(null!, _uowMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }
}
