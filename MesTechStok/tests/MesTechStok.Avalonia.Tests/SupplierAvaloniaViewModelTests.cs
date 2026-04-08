using FluentAssertions;
using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Application.Features.Crm.Queries.GetSuppliersCrm;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class SupplierAvaloniaViewModelTests
{
    private static readonly Guid TestTenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private static readonly GetSuppliersCrmResult DemoResult = new()
    {
        TotalCount = 3,
        Items = new List<SupplierCrmDto>
        {
            new() { Id = Guid.NewGuid(), Name = "ABC Elektronik Ltd.", Email = "info@abcelektronik.com", Phone = "02121234567", City = "Istanbul", IsActive = true, CurrentBalance = 125000.00m },
            new() { Id = Guid.NewGuid(), Name = "Mega Bilisim A.S.", Email = "satis@megabilisim.com", Phone = "03124567890", City = "Ankara", IsActive = true, CurrentBalance = 87500.00m },
            new() { Id = Guid.NewGuid(), Name = "Deniz Teknoloji", Email = "iletisim@deniztek.com", Phone = "02327654321", City = "Izmir", IsActive = true, CurrentBalance = 43200.00m },
        }
    };

    private static SupplierAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetSuppliersCrmQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DemoResult);

        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(u => u.TenantId).Returns(TestTenantId);

        return new SupplierAvaloniaViewModel(mediatorMock.Object, currentUserMock.Object);
    }

    // ── 3-State: Default ──

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.SearchText.Should().BeEmpty();
        sut.TotalCount.Should().Be(0);
        sut.Suppliers.Should().BeEmpty();
    }

    // ── 3-State: Loading → Loaded ──

    [Fact]
    public async Task LoadAsync_ShouldPopulateSuppliersAndSetCounts()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.Suppliers.Should().HaveCount(3);
        sut.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task LoadAsync_SupplierData_ShouldContainExpectedFields()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        var first = sut.Suppliers[0];
        first.SupplierName.Should().Be("ABC Elektronik Ltd.");
        first.ContactPerson.Should().BeEmpty(); // VM maps ContactPerson as empty
        first.Email.Should().Contain("@");
        first.City.Should().Be("Istanbul");
        first.Balance.Should().Be(125000.00m);
    }

    // ── 3-State: Refresh ──

    [Fact]
    public async Task RefreshCommand_ShouldReloadSuppliers()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        await sut.RefreshCommand.ExecuteAsync(null);

        // Assert
        sut.Suppliers.Should().HaveCount(3);
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_MultipleCalls_ShouldNotDuplicateData()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();
        await sut.LoadAsync();

        // Assert — collection should be cleared and reloaded, not appended
        sut.Suppliers.Should().HaveCount(3);
        sut.TotalCount.Should().Be(3);
    }
}
