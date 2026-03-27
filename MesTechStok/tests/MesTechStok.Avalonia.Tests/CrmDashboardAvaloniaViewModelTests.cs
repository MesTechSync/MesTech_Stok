using FluentAssertions;
using MesTech.Application.DTOs.Crm;
using MesTech.Application.Features.Crm.Queries.GetCrmDashboard;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class CrmDashboardAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly CrmDashboardAvaloniaViewModel _sut;

    public CrmDashboardAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new CrmDashboardAvaloniaViewModel(_mediatorMock.Object, Mock.Of<MesTech.Domain.Interfaces.ICurrentUserService>());
    }

    private CrmDashboardDto CreateTestDto() => new()
    {
        TotalCustomers = 150,
        ActiveCustomers = 25,
        VipCustomers = 12,
        PipelineValue = 450_000m,
        OpenDeals = 18,
        UnreadMessages = 7,
        TotalMessages = 30,
        TotalLeads = 42,
        StageSummaries =
        [
            new StageSummaryDto { StageName = "Yeni", DealCount = 5, TotalValue = 100_000m, StageColor = "#3B82F6" },
            new StageSummaryDto { StageName = "Gorusme", DealCount = 8, TotalValue = 200_000m, StageColor = "#10B981" }
        ],
        RecentActivities =
        [
            new MesTech.Application.DTOs.Crm.RecentActivityDto { ContactName = "Ahmet", Subject = "Teklif gonderildi", OccurredAt = DateTime.Now.AddMinutes(-30), Type = "Deal" }
        ]
    };

    [Fact]
    public async Task LoadAsync_ShouldMapKPIsFromMediator()
    {
        // Arrange
        var dto = CreateTestDto();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCrmDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.TotalCustomers.Should().Be(150);
        _sut.NewThisMonth.Should().Be(25);
        _sut.VipCustomers.Should().Be(12);
        _sut.OpenDeals.Should().Be(18);
        _sut.TotalLeads.Should().Be(42);
        _sut.PipelineValue.Should().Contain("450");
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulatePipelineSummary()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCrmDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestDto());

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.PipelineSummary.Should().HaveCount(2);
        _sut.PipelineSummary[0].Stage.Should().Be("Yeni");
        _sut.PipelineSummary[0].Count.Should().Be(5);
        _sut.PipelineSummary[1].Stage.Should().Be("Gorusme");
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateRecentActivities()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCrmDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestDto());

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.RecentActivities.Should().HaveCount(1);
        _sut.RecentActivities[0].Description.Should().Contain("Ahmet");
        _sut.RecentActivities[0].Description.Should().Contain("Teklif gonderildi");
        _sut.RecentActivities[0].Type.Should().Be("Deal");
    }

    [Fact]
    public async Task LoadAsync_WhenMediatorThrows_ShouldSetErrorState()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCrmDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB baglantisi kesildi"));

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.HasError.Should().BeTrue();
        _sut.ErrorMessage.Should().Contain("DB baglantisi kesildi");
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenNoData_ShouldSetIsEmpty()
    {
        // Arrange
        var emptyDto = new CrmDashboardDto
        {
            TotalCustomers = 0,
            OpenDeals = 0,
            StageSummaries = [],
            RecentActivities = []
        };
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCrmDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyDto);

        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.IsEmpty.Should().BeTrue();
        _sut.TotalCustomers.Should().Be(0);
    }
}
