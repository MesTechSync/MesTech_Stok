using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class MultiTenantAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private MultiTenantAvaloniaViewModel CreateSut() => new(_mediatorMock.Object);

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
        sut.ActiveTenantName.Should().Be("MesTech Ana");
        sut.ActiveTenantId.Should().Be("tenant-001");
        sut.Tenants.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulate3Tenants()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Tenants.Should().HaveCount(3);
        sut.IsEmpty.Should().BeFalse();
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldSetActiveTenantInfo()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.ActiveTenantName.Should().Be("MesTech Ana");
        sut.ActiveTenantId.Should().Be("tenant-001");
    }

    [Fact]
    public async Task LoadAsync_TenantsShouldHaveCorrectStatuses()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        var active = sut.Tenants.Where(t => t.Status == "Aktif").ToList();
        var passive = sut.Tenants.Where(t => t.Status == "Pasif").ToList();
        active.Should().HaveCount(2);
        passive.Should().HaveCount(1);
        passive[0].Name.Should().Be("Demo Firma");
    }

    [Fact]
    public async Task LoadAsync_3StateTransition_ShouldEndInSuccessState()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.IsEmpty.Should().BeFalse();
        sut.Tenants.Should().AllSatisfy(t =>
        {
            t.Name.Should().NotBeNullOrEmpty();
            t.Database.Should().NotBeNullOrEmpty();
            t.Status.Should().NotBeNullOrEmpty();
        });
    }
}
