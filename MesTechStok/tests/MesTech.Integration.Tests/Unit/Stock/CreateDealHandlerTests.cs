using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.CreateDeal;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// CreateDealHandler: CRM fırsat/teklif oluşturma.
/// Deal.Create factory + persistence.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "CrmChain")]
public class CreateDealHandlerTests
{
    private readonly Mock<ICrmDealRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public CreateDealHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _repo.Setup(r => r.AddAsync(It.IsAny<Deal>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
    }

    private CreateDealHandler CreateHandler() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Handle_ValidDeal_ReturnsGuidAndPersists()
    {
        var cmd = new CreateDealCommand(
            TenantId: Guid.NewGuid(),
            Title: "Büyük Sipariş Fırsatı",
            PipelineId: Guid.NewGuid(),
            StageId: Guid.NewGuid(),
            Amount: 50000m);

        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        _repo.Verify(r => r.AddAsync(It.IsAny<Deal>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var handler = CreateHandler();
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithOptionalFields_StillCreates()
    {
        var cmd = new CreateDealCommand(
            TenantId: Guid.NewGuid(),
            Title: "İsteğe Bağlı Alanlar",
            PipelineId: Guid.NewGuid(),
            StageId: Guid.NewGuid(),
            Amount: 0m,
            CrmContactId: Guid.NewGuid(),
            ExpectedCloseDate: DateTime.UtcNow.AddDays(30),
            AssignedToUserId: Guid.NewGuid(),
            StoreId: Guid.NewGuid());

        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
    }
}
