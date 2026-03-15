using FluentAssertions;
using MediatR;
using MesTech.Application.Features.Hr.Commands.ApproveLeave;
using MesTech.Domain.Entities.Hr;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Hr;

/// <summary>
/// DEV 5 — H31 Task 5.3: ApproveLeaveHandler unit tests.
/// Verifies approval of pending leave, not-found exception,
/// and double-approve exception.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Hr")]
public class HrHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _employeeId = Guid.NewGuid();
    private static readonly Guid _approverUserId = Guid.NewGuid();

    private readonly Mock<ILeaveRepository> _leaveRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ApproveLeaveHandler CreateHandler() =>
        new(_leaveRepo.Object, _uow.Object);

    /// <summary>
    /// Test 1: A pending leave should be successfully approved.
    /// After approval, Status = Approved, ApprovedByUserId set, ApprovedAt set.
    /// </summary>
    [Fact]
    public async Task ApproveLeaveHandler_PendingLeave_ShouldApprove()
    {
        // Arrange
        var leave = Leave.Create(
            _tenantId, _employeeId, LeaveType.Annual,
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10),
            "Tatil izni");

        leave.Status.Should().Be(LeaveStatus.Pending, "newly created leave must be Pending");

        _leaveRepo.Setup(r => r.GetByIdAsync(leave.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leave);

        var command = new ApproveLeaveCommand(leave.Id, _approverUserId);
        var handler = CreateHandler();

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        leave.Status.Should().Be(LeaveStatus.Approved,
            "leave status must change to Approved after handler execution");
        leave.ApprovedByUserId.Should().Be(_approverUserId,
            "ApprovedByUserId must be set to the approver");
        leave.ApprovedAt.Should().NotBeNull(
            "ApprovedAt must be set after approval");

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once,
            "UnitOfWork.SaveChangesAsync must be called once");
    }

    /// <summary>
    /// Test 2: If leave ID not found, handler should throw InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task ApproveLeaveHandler_NotFound_ShouldThrow()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _leaveRepo.Setup(r => r.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Leave?)null);

        var command = new ApproveLeaveCommand(nonExistentId, _approverUserId);
        var handler = CreateHandler();

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{nonExistentId}*",
                "exception message must contain the missing leave ID");

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never,
            "UnitOfWork.SaveChangesAsync must NOT be called when leave is not found");
    }

    /// <summary>
    /// Test 3: Attempting to approve an already-approved leave should throw.
    /// The Leave.Approve() method guards: "Only pending leaves can be approved."
    /// </summary>
    [Fact]
    public async Task ApproveLeaveHandler_AlreadyApproved_ShouldThrow()
    {
        // Arrange
        var leave = Leave.Create(
            _tenantId, _employeeId, LeaveType.Annual,
            DateTime.UtcNow.AddDays(5), DateTime.UtcNow.AddDays(10),
            "Yillik izin");

        // First approval (changes status to Approved)
        leave.Approve(Guid.NewGuid());
        leave.Status.Should().Be(LeaveStatus.Approved, "leave must already be Approved");

        _leaveRepo.Setup(r => r.GetByIdAsync(leave.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(leave);

        var command = new ApproveLeaveCommand(leave.Id, _approverUserId);
        var handler = CreateHandler();

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pending*",
                "exception should indicate only pending leaves can be approved");
    }
}
