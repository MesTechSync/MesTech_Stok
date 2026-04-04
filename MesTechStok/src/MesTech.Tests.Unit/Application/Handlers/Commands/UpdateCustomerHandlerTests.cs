using FluentAssertions;
using MesTech.Application.Commands.UpdateCustomer;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: UpdateCustomerHandler testi — müşteri güncelleme.
/// P1: Müşteri bilgileri sipariş + fatura zincirinde kullanılır.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class UpdateCustomerHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateCustomerHandler CreateSut() => new(_customerRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_CustomerNotFound_ShouldReturnFailure()
    {
        _customerRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Customer?)null);
        var cmd = new UpdateCustomerCommand(Guid.NewGuid(), "Test", "TST");

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldUpdateAllFieldsAndSave()
    {
        var customer = new Customer { Name = "Old", Code = "OLD", TenantId = Guid.NewGuid() };
        _customerRepo.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var cmd = new UpdateCustomerCommand(
            customer.Id, "Yeni Müşteri", "YM-01",
            ContactPerson: "Ali Veli",
            Email: "ali@test.com",
            Phone: "05551234567",
            City: "İstanbul",
            TaxNumber: "1234567890",
            PaymentTermDays: 30);

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.CustomerId.Should().Be(customer.Id);
        customer.Name.Should().Be("Yeni Müşteri");
        customer.Code.Should().Be("YM-01");
        customer.ContactPerson.Should().Be("Ali Veli");
        customer.Email.Should().Be("ali@test.com");
        customer.City.Should().Be("İstanbul");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Deactivation_ShouldSetIsActiveFalse()
    {
        var customer = new Customer { Name = "Active", Code = "ACT", IsActive = true, TenantId = Guid.NewGuid() };
        _customerRepo.Setup(r => r.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>())).ReturnsAsync(customer);

        var cmd = new UpdateCustomerCommand(customer.Id, "Active", "ACT", IsActive: false);
        await CreateSut().Handle(cmd, CancellationToken.None);

        customer.IsActive.Should().BeFalse();
    }
}
