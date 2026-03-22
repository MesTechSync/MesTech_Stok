using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.CreateLead;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "CrmLeadCommands")]
public class CreateLeadHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateLeadAndReturnId()
    {
        // Arrange
        Lead? capturedLead = null;
        var mockRepo = new Mock<ICrmLeadRepository>();
        mockRepo.Setup(r => r.AddAsync(It.IsAny<Lead>(), It.IsAny<CancellationToken>()))
            .Callback<Lead, CancellationToken>((l, _) => capturedLead = l);
        var mockUow = new Mock<IUnitOfWork>();

        var handler = new CreateLeadHandler(mockRepo.Object, mockUow.Object);
        var command = new CreateLeadCommand(
            _tenantId, "Ali Yilmaz", LeadSource.Web,
            "ali@example.com", "+905551234567", "AliCo");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        capturedLead.Should().NotBeNull();
        capturedLead!.FullName.Should().Be("Ali Yilmaz");
        capturedLead.Source.Should().Be(LeadSource.Web);
        capturedLead.Status.Should().Be(LeadStatus.New);
        mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MinimalFields_ShouldCreateLead()
    {
        // Arrange
        var mockRepo = new Mock<ICrmLeadRepository>();
        var mockUow = new Mock<IUnitOfWork>();
        var handler = new CreateLeadHandler(mockRepo.Object, mockUow.Object);
        var command = new CreateLeadCommand(_tenantId, "Minimal Lead", LeadSource.Manual);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        mockRepo.Verify(r => r.AddAsync(It.IsAny<Lead>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        // Arrange
        var handler = new CreateLeadHandler(
            Mock.Of<ICrmLeadRepository>(), Mock.Of<IUnitOfWork>());

        // Act
        var act = () => handler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
