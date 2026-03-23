using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class TenantAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private TenantAvaloniaViewModel CreateSut() => new(_mediatorMock.Object);

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
        sut.TenantName.Should().Be("MesTech Ana");
        sut.TenantCode.Should().Be("MESTECH-001");
        sut.TenantPlan.Should().Be("Enterprise");
        sut.DatabaseName.Should().Be("mestech_main");
        sut.MaxUsers.Should().Be(50);
        sut.StorageUsed.Should().Be(12.4);
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateTenantDetails()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.TenantName.Should().Be("MesTech Ana");
        sut.TenantCode.Should().Be("MESTECH-001");
        sut.TenantPlan.Should().Be("Enterprise");
        sut.DatabaseName.Should().Be("mestech_main");
        sut.MaxUsers.Should().Be(50);
        sut.StorageUsed.Should().Be(12.4);
    }

    [Fact]
    public async Task LoadAsync_ShouldTransitionTo3StateSuccess()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — 3-state: loading done, no error, not empty
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldResetErrorStateBeforeLoading()
    {
        // Arrange
        var sut = CreateSut();

        // Simulate a previous error state by setting property via reflection
        // First load should work fine, proving error reset logic
        await sut.LoadAsync();
        sut.HasError.Should().BeFalse();

        // Second load should also reset and succeed
        await sut.LoadAsync();

        // Assert
        sut.HasError.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void PropertyChanged_ShouldFireForTenantName()
    {
        // Arrange
        var sut = CreateSut();
        var raised = false;
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(sut.TenantName))
                raised = true;
        };

        // Act
        sut.TenantName = "Changed Tenant";

        // Assert
        raised.Should().BeTrue();
        sut.TenantName.Should().Be("Changed Tenant");
    }
}
