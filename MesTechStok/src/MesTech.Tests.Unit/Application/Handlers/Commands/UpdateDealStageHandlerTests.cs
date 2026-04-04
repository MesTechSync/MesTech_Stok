using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.UpdateDealStage;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: UpdateDealStageHandler testi — CRM fırsat aşama güncelleme.
/// P1: CRM pipeline yönetimi satış takibi için kritik.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class UpdateDealStageHandlerTests
{
    private readonly Mock<IDealRepository> _dealRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<UpdateDealStageHandler>> _logger = new();

    private UpdateDealStageHandler CreateSut() => new(_dealRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task Handle_DealNotFound_ShouldReturnFailure()
    {
        _dealRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Deal?)null);

        var cmd = new UpdateDealStageCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bulunamadi");
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldUpdateStageAndSave()
    {
        var deal = Deal.Create(Guid.NewGuid(), "Test Deal", Guid.NewGuid(), Guid.NewGuid(), 5000m);
        _dealRepo.Setup(r => r.GetByIdAsync(deal.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        var newStageId = Guid.NewGuid();
        var cmd = new UpdateDealStageCommand(deal.Id, newStageId, deal.TenantId);
        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deal.StageId.Should().Be(newStageId);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
