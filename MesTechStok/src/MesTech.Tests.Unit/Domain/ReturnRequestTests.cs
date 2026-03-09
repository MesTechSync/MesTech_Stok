using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using Xunit;

namespace MesTech.Tests.Unit.Domain;

/// <summary>
/// ReturnRequest entity unit tests — state transitions + domain events.
/// 4 tests: default Pending, Approve, Reject, guard clause.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "ReturnRequest")]
public class ReturnRequestTests
{
    private static ReturnRequest CreatePendingReturn()
    {
        return ReturnRequest.Create(
            orderId: Guid.NewGuid(),
            tenantId: Guid.NewGuid(),
            platform: PlatformType.Trendyol,
            reason: ReturnReason.DefectiveProduct,
            customerName: "Test Musteri",
            reasonDetail: "Urun hasarli geldi");
    }

    // ════ 1. Default status is Pending ════

    [Fact]
    public void NewReturnRequest_DefaultStatus_IsPending()
    {
        // Act
        var returnRequest = CreatePendingReturn();

        // Assert
        returnRequest.Status.Should().Be(ReturnStatus.Pending);
        returnRequest.ApprovedAt.Should().BeNull();
        returnRequest.Reason.Should().Be(ReturnReason.DefectiveProduct);
        returnRequest.CustomerName.Should().Be("Test Musteri");
        returnRequest.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ReturnCreatedEvent>();
    }

    // ════ 2. Approve transitions Pending → Approved ════

    [Fact]
    public void Approve_PendingReturn_TransitionsToApproved()
    {
        // Arrange
        var returnRequest = CreatePendingReturn();

        // Act
        returnRequest.Approve();

        // Assert
        returnRequest.Status.Should().Be(ReturnStatus.Approved);
        returnRequest.ApprovedAt.Should().NotBeNull();
        returnRequest.ApprovedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ════ 3. Reject transitions Pending → Rejected ════

    [Fact]
    public void Reject_PendingReturn_TransitionsToRejected()
    {
        // Arrange
        var returnRequest = CreatePendingReturn();

        // Act
        returnRequest.Reject("Sehven acilmis talep");

        // Assert
        returnRequest.Status.Should().Be(ReturnStatus.Rejected);
        returnRequest.Notes.Should().Be("Sehven acilmis talep");
    }

    // ════ 4. Approve already rejected → throws ════

    [Fact]
    public void Approve_AlreadyRejected_ThrowsInvalidOperation()
    {
        // Arrange
        var returnRequest = CreatePendingReturn();
        returnRequest.Reject("Gecersiz talep");

        // Act & Assert
        var act = () => returnRequest.Approve();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*bekleyen*");
    }
}
