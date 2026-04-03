using FluentAssertions;
using MesTech.Application.Features.Hr.Commands.CreateTimeEntry;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class CreateTimeEntryHandlerTests
{
    private readonly Mock<ITimeEntryRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateTimeEntryHandler CreateHandler() =>
        new(_repository.Object, _unitOfWork.Object);

    [Fact]
    public void Constructor_NullRepository_ShouldThrow()
    {
        var act = () => new CreateTimeEntryHandler(null!, _unitOfWork.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_NullUnitOfWork_ShouldThrow()
    {
        var act = () => new CreateTimeEntryHandler(_repository.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("unitOfWork");
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateEntryAndReturnId()
    {
        var tenantId = Guid.NewGuid();
        var workTaskId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new CreateTimeEntryCommand(tenantId, workTaskId, userId, "Dev work", true, 50m);

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _repository.Verify(r => r.AddAsync(It.Is<TimeEntry>(e =>
            e.TenantId == tenantId &&
            e.WorkTaskId == workTaskId &&
            e.UserId == userId &&
            e.IsBillable == true &&
            e.HourlyRate == 50m
        ), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MinimalCommand_ShouldCreateEntryWithDefaults()
    {
        var command = new CreateTimeEntryCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var handler = CreateHandler();
        var result = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _repository.Verify(r => r.AddAsync(It.Is<TimeEntry>(e =>
            e.IsBillable == false &&
            e.HourlyRate == null
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
