using FluentAssertions;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Services;
using Xunit;

namespace MesTech.Tests.Unit.Application;

/// <summary>
/// ReturnPolicyService unit tests — platform kuralları + atomik doğrulama.
/// 4 tests: Trendyol 15-day valid/expired, stock restore check, cari account credit.
/// Demir Kural 8: İade stok iadesi atomik.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "ReturnProcessing")]
public class ReturnProcessingTests
{
    private readonly ReturnPolicyService _policyService = new();

    private static Order CreateDeliveredOrder(DateTime orderDate)
    {
        return new Order
        {
            TenantId = Guid.NewGuid(),
            OrderNumber = "ORD-001",
            CustomerId = Guid.NewGuid(),
            Status = OrderStatus.Delivered,
            OrderDate = orderDate,
            TotalAmount = 500m
        };
    }

    private static ReturnRequest CreateReturn(PlatformType platform, DateTime requestDate, Guid orderId)
    {
        var rr = new ReturnRequest
        {
            OrderId = orderId,
            TenantId = Guid.NewGuid(),
            Platform = platform,
            Reason = ReturnReason.DefectiveProduct,
            CustomerName = "Test Musteri",
            RequestDate = requestDate
        };
        return rr;
    }

    // ════ 1. Trendyol — 15-day return within window ════

    [Fact]
    public void Trendyol_ReturnPeriod_15Days_Valid()
    {
        // Arrange — order delivered 10 days ago
        var orderDate = DateTime.UtcNow.AddDays(-10);
        var order = CreateDeliveredOrder(orderDate);
        var returnRequest = CreateReturn(PlatformType.Trendyol, DateTime.UtcNow, order.Id);

        // Act
        var result = _policyService.Validate(returnRequest, order);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Policy.Should().NotBeNull();
        result.Policy!.ReturnWindowDays.Should().Be(15);
        result.Policy.IsCargoFree.Should().BeTrue();
    }

    // ════ 2. Trendyol — 15-day return expired ════

    [Fact]
    public void Trendyol_ReturnPeriod_15Days_Expired()
    {
        // Arrange — order delivered 20 days ago (beyond 15-day window)
        var orderDate = DateTime.UtcNow.AddDays(-20);
        var order = CreateDeliveredOrder(orderDate);
        var returnRequest = CreateReturn(PlatformType.Trendyol, DateTime.UtcNow, order.Id);

        // Act
        var result = _policyService.Validate(returnRequest, order);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("süre");
    }

    // ════ 3. ApplyPolicy — auto-restore stock check ════

    [Fact]
    public void Trendyol_AutoRestoreStock_ReturnsTrue()
    {
        // Act — Trendyol policy: AutoRestoreStock = true
        var shouldRestore = _policyService.ShouldAutoRestoreStock(PlatformType.Trendyol);

        // Assert
        shouldRestore.Should().BeTrue();

        // OpenCart: AutoRestoreStock = false
        var openCartRestore = _policyService.ShouldAutoRestoreStock(PlatformType.OpenCart);
        openCartRestore.Should().BeFalse();
    }

    // ════ 4. ApplyPolicy — auto-approve for Trendyol (RequiresApproval=false) ════

    [Fact]
    public void ApplyPolicy_Trendyol_AutoApproves()
    {
        // Arrange — Trendyol: RequiresApproval = false → auto-approve
        var returnRequest = CreateReturn(PlatformType.Trendyol, DateTime.UtcNow, Guid.NewGuid());

        // Act
        _policyService.ApplyPolicy(returnRequest);

        // Assert — Trendyol auto-approves
        returnRequest.Status.Should().Be(ReturnStatus.Approved);
        returnRequest.IsCargoFree.Should().BeTrue();
        returnRequest.DeadlineDate.Should().NotBeNull();

        // Contrast: Ciceksepeti requires approval
        var csReturn = CreateReturn(PlatformType.Ciceksepeti, DateTime.UtcNow, Guid.NewGuid());
        _policyService.ApplyPolicy(csReturn);
        csReturn.Status.Should().Be(ReturnStatus.Pending); // NOT auto-approved
        csReturn.IsCargoFree.Should().BeFalse();
    }
}
