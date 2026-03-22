using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.CreateDeal;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "CrmDealCommands")]
public class CreateDealHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _pipelineId = Guid.NewGuid();
    private static readonly Guid _stageId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateDealAndReturnId()
    {
        // Arrange
        Deal? capturedDeal = null;
        var mockRepo = new Mock<ICrmDealRepository>();
        mockRepo.Setup(r => r.AddAsync(It.IsAny<Deal>(), It.IsAny<CancellationToken>()))
            .Callback<Deal, CancellationToken>((d, _) => capturedDeal = d);
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new CreateDealHandler(mockRepo.Object, mockUow.Object);
        var command = new CreateDealCommand(_tenantId, "New Deal", _pipelineId, _stageId, 10000m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        capturedDeal.Should().NotBeNull();
        capturedDeal!.Title.Should().Be("New Deal");
        capturedDeal.Amount.Should().Be(10000m);
        capturedDeal.Status.Should().Be(DealStatus.Open);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithOptionalFields_ShouldSetAllProperties()
    {
        // Arrange
        Deal? capturedDeal = null;
        var contactId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var storeId = Guid.NewGuid();
        var closeDate = DateTime.UtcNow.AddDays(30);
        var mockRepo = new Mock<ICrmDealRepository>();
        mockRepo.Setup(r => r.AddAsync(It.IsAny<Deal>(), It.IsAny<CancellationToken>()))
            .Callback<Deal, CancellationToken>((d, _) => capturedDeal = d);
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new CreateDealHandler(mockRepo.Object, mockUow.Object);
        var command = new CreateDealCommand(
            _tenantId, "Full Deal", _pipelineId, _stageId, 25000m,
            contactId, closeDate, userId, storeId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        capturedDeal.Should().NotBeNull();
        capturedDeal!.CrmContactId.Should().Be(contactId);
        capturedDeal.ExpectedCloseDate.Should().Be(closeDate);
        capturedDeal.AssignedToUserId.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        // Arrange
        var handler = new CreateDealHandler(
            Mock.Of<ICrmDealRepository>(), Mock.Of<IUnitOfWork>());

        // Act
        var act = () => handler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
