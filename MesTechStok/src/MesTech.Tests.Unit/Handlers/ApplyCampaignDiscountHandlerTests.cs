using FluentAssertions;
using MesTech.Application.Features.Crm.Queries.ApplyCampaignDiscount;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class ApplyCampaignDiscountHandlerTests
{
    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var sut = new ApplyCampaignDiscountHandler(Mock.Of<ICampaignRepository>(), new PricingService());
        var act = () => sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
