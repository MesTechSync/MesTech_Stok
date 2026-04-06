using FluentAssertions;
using MesTech.Application.Commands.CreateCustomer;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateCustomerHandlerTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ITenantProvider> _tenantProviderMock = new();
    private readonly CreateCustomerHandler _sut;

    public CreateCustomerHandlerTests()
    {
        _tenantProviderMock.Setup(t => t.GetCurrentTenantId()).Returns(TestTenantId);
        _sut = new CreateCustomerHandler(_customerRepoMock.Object, _uowMock.Object, _tenantProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesCustomerAndReturnsSuccess()
    {
        var cmd = new CreateCustomerCommand("Test Müşteri", "MUS-001", Email: "info@test.com");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.CustomerId.Should().NotBe(Guid.Empty);

        _customerRepoMock.Verify(r => r.AddAsync(It.Is<Customer>(c =>
            c.Name == "Test Müşteri" &&
            c.Code == "MUS-001" &&
            c.Email == "info@test.com"), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DefaultValues_SetsCorrectDefaults()
    {
        var cmd = new CreateCustomerCommand("Müşteri", "MUS-002");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _customerRepoMock.Verify(r => r.AddAsync(It.Is<Customer>(c =>
            c.CustomerType == "INDIVIDUAL" &&
            c.PaymentTermDays == 0 &&
            c.IsActive == true), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsTenantIdFromProvider()
    {
        var cmd = new CreateCustomerCommand("Tenant Test", "MUS-003");

        await _sut.Handle(cmd, CancellationToken.None);

        _customerRepoMock.Verify(r => r.AddAsync(It.Is<Customer>(c =>
            c.TenantId == TestTenantId), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_NullRepository_Throws()
    {
        var act = () => new CreateCustomerHandler(null!, _uowMock.Object, _tenantProviderMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }
}
