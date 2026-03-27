using FluentAssertions;
using MesTech.Application.Queries.GetCategories;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetCategoriesHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCategories()
    {
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Category>().AsReadOnly());
        var sut = new GetCategoriesHandler(repo.Object);

        var query = new GetCategoriesQuery();
        var result = await sut.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
        repo.Verify(r => r.GetAllAsync(), Times.Once);
    }
}
