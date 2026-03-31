using FluentAssertions;
using MesTech.Application.Commands.CreateSupplier;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class CreateSupplierHandlerTests
{
    private static readonly Guid TestTenantId = Guid.NewGuid();
    private readonly Mock<ISupplierRepository> _supplierRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ITenantProvider> _tenantProviderMock = new();
    private readonly CreateSupplierHandler _sut;

    public CreateSupplierHandlerTests()
    {
        _tenantProviderMock.Setup(t => t.GetCurrentTenantId()).Returns(TestTenantId);
        _sut = new CreateSupplierHandler(_supplierRepoMock.Object, _uowMock.Object, _tenantProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesSupplierAndReturnsSuccess()
    {
        var cmd = new CreateSupplierCommand("Test Tedarikçi", "TDK-001", Email: "info@tedarik.com");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.SupplierId.Should().NotBe(Guid.Empty);

        _supplierRepoMock.Verify(r => r.AddAsync(It.Is<Supplier>(s =>
            s.Name == "Test Tedarikçi" &&
            s.Code == "TDK-001")), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AllOptionals_SetsCorrectValues()
    {
        var cmd = new CreateSupplierCommand(
            "Tam Tedarikçi", "TDK-002",
            ContactPerson: "Ali Bey",
            Email: "ali@tedarik.com",
            Phone: "05551234567",
            City: "İstanbul",
            TaxNumber: "1234567890",
            TaxOffice: "Beyoğlu VD");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _supplierRepoMock.Verify(r => r.AddAsync(It.Is<Supplier>(s =>
            s.ContactPerson == "Ali Bey" &&
            s.City == "İstanbul" &&
            s.TaxNumber == "1234567890")), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullRepository_Throws()
    {
        var act = () => new CreateSupplierHandler(null!, _uowMock.Object, _tenantProviderMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }
}
