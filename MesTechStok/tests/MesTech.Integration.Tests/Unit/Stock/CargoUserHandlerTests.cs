using FluentAssertions;
using MesTech.Application.Features.Cargo.Queries.GetCargoTrackingList;
using MesTech.Application.Features.Users.Queries.GetUsers;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

[Trait("Category", "Unit")]
[Trait("Layer", "Cargo")]
[Trait("Group", "Handler")]
public class CargoUserHandlerTests
{
    // ═══ GetCargoTrackingList ═══

    [Fact]
    public async Task GetCargoTrackingList_NullRequest_Throws()
    {
        var repo = new Mock<IOrderRepository>();
        var handler = new GetCargoTrackingListHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetUsers ═══

    [Fact]
    public async Task GetUsers_NullRequest_Throws()
    {
        var repo = new Mock<IUserRepository>();
        var logger = Mock.Of<ILogger<GetUsersHandler>>();
        var handler = new GetUsersHandler(repo.Object, logger);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }
}
