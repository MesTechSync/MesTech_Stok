using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class HealthAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private HealthAvaloniaViewModel CreateSut() => new(Mock.Of<IMediator>(), Mock.Of<MesTech.Domain.Interfaces.ICurrentUserService>());

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
        sut.CpuUsage.Should().Be(0);
        sut.RamUsage.Should().Be(0);
        sut.DiskUsage.Should().Be(0);
        sut.LastUpdated.Should().Be("--:--");
        sut.ServiceStatuses.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateMetrics()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.CpuUsage.Should().Be(42);
        sut.RamUsage.Should().Be(68);
        sut.DiskUsage.Should().Be(55);
        sut.LastUpdated.Should().NotBe("--:--");
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulate5Services()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.ServiceStatuses.Should().HaveCount(5);
        sut.ServiceStatuses.Should().AllSatisfy(s => s.Status.Should().Be("Aktif"));
        sut.ServiceStatuses.Select(s => s.ServiceName)
            .Should().Contain(new[] { "PostgreSQL", "Redis", "RabbitMQ" });
    }

    [Fact]
    public async Task LoadAsync_ShouldClearErrorState()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — 3-state verification
        sut.HasError.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.IsEmpty.Should().BeFalse();
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ServiceResponseTimesShouldNotBeEmpty()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.ServiceStatuses.Should().AllSatisfy(s =>
        {
            s.ResponseTime.Should().NotBeNullOrEmpty();
            s.LastCheck.Should().NotBeNullOrEmpty();
            s.ServiceName.Should().NotBeNullOrEmpty();
        });
    }
}
