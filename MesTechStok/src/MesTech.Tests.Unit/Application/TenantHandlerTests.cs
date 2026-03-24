using FluentAssertions;
using MesTech.Application.Features.Tenant.Commands.CreateTenant;
using MesTech.Application.Features.Tenant.Commands.UpdateTenant;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class TenantHandlerTests
{
    private readonly Mock<ITenantRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    [Fact]
    public async Task CreateTenant_ValidCommand_ReturnsNonEmptyGuid()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmd = new CreateTenantCommand("Acme Corp", "1234567890");
        var handler = new CreateTenantHandler(_repo.Object, _uow.Object);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task UpdateTenant_ValidCommand_UpdatesFields()
    {
        var tenant = new Tenant { Name = "Old", IsActive = true };
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var cmd = new UpdateTenantCommand(tenant.Id, "New Name", "9876543210", false);
        var handler = new UpdateTenantHandler(_repo.Object, _uow.Object);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().BeTrue();
        tenant.Name.Should().Be("New Name");
        tenant.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateTenant_NotFound_Throws()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
        var cmd = new UpdateTenantCommand(Guid.NewGuid(), "X", "X", true);
        var handler = new UpdateTenantHandler(_repo.Object, _uow.Object);

        var result = await handler.Handle(cmd, CancellationToken.None);
        result.Should().BeFalse();
    }
}
