using FluentAssertions;
using MesTech.Application.Features.Crm.Queries.GetBitrix24Deals;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetBitrix24DealsHandlerTests
{
    [Fact]
    public void Constructor_NullRepository_Throws()
    {
        var act = () => new GetBitrix24DealsHandler(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
