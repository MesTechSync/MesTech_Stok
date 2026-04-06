using FluentAssertions;
using MesTech.Application.Features.Tenant.Queries.GetTenants;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class MultiTenantAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private MultiTenantAvaloniaViewModel CreateSut()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetTenantsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetTenantsResult(
                Items: Array.Empty<MesTech.Application.DTOs.TenantDto>(),
                TotalCount: 0,
                Page: 1,
                PageSize: 50));
        return new MultiTenantAvaloniaViewModel(_mediatorMock.Object);
    }

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
    public async Task LoadAsync_WithEmptyData_ShouldCompleteWithoutError()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Tenants.Should().BeEmpty();
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldRetainDefaultActiveTenantWhenEmpty()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — no tenants returned, defaults unchanged
        sut.ActiveTenantName.Should().Be("MesTech Ana");
        sut.ActiveTenantId.Should().Be("tenant-001");
    }
}
