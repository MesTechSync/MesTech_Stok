using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.CreateCampaign;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateCampaignHandlerTests
{
    private readonly Mock<ICampaignRepository> _campaignRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateCampaignHandler _sut;

    public CreateCampaignHandlerTests()
    {
        _sut = new CreateCampaignHandler(_campaignRepoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
