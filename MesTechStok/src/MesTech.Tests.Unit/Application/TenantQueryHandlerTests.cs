using FluentAssertions;
using MesTech.Application.Features.Tenant.Queries.GetTenant;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class TenantQueryHandlerTests
{
    [Fact]
    public async Task GetTenant_ExistingTenant_ReturnsDto()
    {
        var repo = new Mock<ITenantRepository>();
        var tenant = new Tenant { Name = "Acme", TaxNumber = "123" };
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var handler = new GetTenantHandler(repo.Object);
        var result = await handler.Handle(
            new GetTenantQuery(TenantId: tenant.Id),
            CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTenant_NotFound_ReturnsNull()
    {
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var handler = new GetTenantHandler(repo.Object);
        var result = await handler.Handle(
            new GetTenantQuery(TenantId: Guid.NewGuid()),
            CancellationToken.None);
        result.Should().BeNull();
    }
}
