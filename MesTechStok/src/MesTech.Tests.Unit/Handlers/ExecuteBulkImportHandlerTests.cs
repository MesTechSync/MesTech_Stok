using FluentAssertions;
using MesTech.Application.Features.Product.Commands.ExecuteBulkImport;
using MesTech.Application.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class ExecuteBulkImportHandlerTests
{
    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var sut = new ExecuteBulkImportHandler(Mock.Of<IBulkProductImportService>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
