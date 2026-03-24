using FluentAssertions;
using MesTech.Application.Features.Tenant.Commands.CreateTenant;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateTenantHandlerTests
{
    private readonly Mock<ITenantRepository> _repo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly CreateTenantHandler _sut;

    public CreateTenantHandlerTests()
    {
        _repo = new Mock<ITenantRepository>();
        _uow = new Mock<IUnitOfWork>();
        _sut = new CreateTenantHandler(_repo.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesTenantAndReturnsGuid()
    {
        var command = new CreateTenantCommand("MesTech AŞ", "1234567890");

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _repo.Verify(r => r.AddAsync(
            It.Is<MesTech.Domain.Entities.Tenant>(t =>
                t.Name == "MesTech AŞ" && t.TaxNumber == "1234567890" && t.IsActive),
            It.IsAny<CancellationToken>()), Times.Once());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NullTaxNumber_StillCreates()
    {
        var command = new CreateTenantCommand("Test Tenant", null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _repo.Verify(r => r.AddAsync(
            It.Is<MesTech.Domain.Entities.Tenant>(t => t.TaxNumber == null),
            It.IsAny<CancellationToken>()), Times.Once());
    }
}
