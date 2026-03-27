using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.EarnPoints;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class EarnPointsHandlerTests
{
    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var sut = new EarnPointsHandler(
            Mock.Of<ILoyaltyProgramRepository>(), Mock.Of<ILoyaltyTransactionRepository>(),
            Mock.Of<IUnitOfWork>());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
