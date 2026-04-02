using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.UpdateDealStage;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

// ════════════════════════════════════════════════════════
// DEV5 TUR 4: CRM Deal handler tests (WinDeal+LoseDeal zaten CrmExtraHandlerTests'te)
// ════════════════════════════════════════════════════════

#region UpdateDealStageHandler

[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
public class UpdateDealStageHandlerTests
{
    private readonly Mock<IDealRepository> _dealRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<UpdateDealStageHandler>> _logger = new();

    private UpdateDealStageHandler CreateSut() => new(_dealRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task Handle_NonExistentDeal_ShouldReturnError()
    {
        _dealRepo.Setup(d => d.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Deal?)null);

        var sut = CreateSut();
        var result = await sut.Handle(
            new UpdateDealStageCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}

#endregion
