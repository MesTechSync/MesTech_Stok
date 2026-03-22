using FluentAssertions;
using MesTech.Application.Features.Crm.Queries.GetDeals;
using MesTech.Application.Features.Crm.Queries.GetPipelineKanban;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "CrmDealQueries")]
public class DealQueryHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _pipelineId = Guid.NewGuid();
    private static readonly Guid _stageId = Guid.NewGuid();

    private static Deal MakeDeal(string title = "Test Deal", decimal amount = 5000m)
        => Deal.Create(_tenantId, title, _pipelineId, _stageId, amount);

    // ── GetDealsHandler ──

    [Fact]
    public async Task GetDeals_ByTenantPaged_ShouldReturnMappedResult()
    {
        // Arrange
        var deals = new List<Deal> { MakeDeal("Deal A", 1000m), MakeDeal("Deal B", 2000m) }.AsReadOnly();
        var mockRepo = new Mock<ICrmDealRepository>();
        mockRepo.Setup(r => r.GetByTenantPagedAsync(_tenantId, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deals);

        var handler = new GetDealsHandler(mockRepo.Object);
        var query = new GetDealsQuery(_tenantId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items[0].Title.Should().Be("Deal A");
        result.Items[1].Amount.Should().Be(2000m);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetDeals_ByPipeline_ShouldUseGetByPipeline()
    {
        // Arrange
        var deals = new List<Deal> { MakeDeal() }.AsReadOnly();
        var mockRepo = new Mock<ICrmDealRepository>();
        mockRepo.Setup(r => r.GetByPipelineAsync(_tenantId, _pipelineId, DealStatus.Open, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deals);

        var handler = new GetDealsHandler(mockRepo.Object);
        var query = new GetDealsQuery(_tenantId, PipelineId: _pipelineId, Status: DealStatus.Open);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        mockRepo.Verify(r => r.GetByPipelineAsync(_tenantId, _pipelineId, DealStatus.Open, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDeals_NullRequest_ShouldThrow()
    {
        var handler = new GetDealsHandler(Mock.Of<ICrmDealRepository>());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── GetPipelineKanbanHandler ──

    [Fact]
    public async Task GetPipelineKanban_ValidPipeline_ShouldReturnKanbanBoard()
    {
        // Arrange
        var pipeline = Pipeline.Create(_tenantId, "Sales Pipeline", true, 1);

        var mockPipelineRepo = new Mock<IPipelineRepository>();
        mockPipelineRepo.Setup(r => r.GetByIdWithStagesAsync(_pipelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);

        var mockDealRepo = new Mock<ICrmDealRepository>();
        mockDealRepo.Setup(r => r.GetByPipelineAsync(_tenantId, _pipelineId, DealStatus.Open, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Deal>().AsReadOnly());

        var handler = new GetPipelineKanbanHandler(mockDealRepo.Object, mockPipelineRepo.Object);
        var query = new GetPipelineKanbanQuery(_tenantId, _pipelineId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.PipelineId.Should().Be(pipeline.Id);
        result.PipelineName.Should().Be("Sales Pipeline");
    }

    [Fact]
    public async Task GetPipelineKanban_PipelineNotFound_ShouldThrow()
    {
        // Arrange
        var mockPipelineRepo = new Mock<IPipelineRepository>();
        mockPipelineRepo.Setup(r => r.GetByIdWithStagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pipeline?)null);

        var handler = new GetPipelineKanbanHandler(Mock.Of<ICrmDealRepository>(), mockPipelineRepo.Object);

        // Act
        var act = () => handler.Handle(new GetPipelineKanbanQuery(_tenantId, _pipelineId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task GetPipelineKanban_EmptyStages_ShouldReturnEmptyBoard()
    {
        // Arrange
        var pipeline = Pipeline.Create(_tenantId, "Empty Pipeline", false, 2);

        var mockPipelineRepo = new Mock<IPipelineRepository>();
        mockPipelineRepo.Setup(r => r.GetByIdWithStagesAsync(_pipelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);

        var mockDealRepo = new Mock<ICrmDealRepository>();
        mockDealRepo.Setup(r => r.GetByPipelineAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DealStatus>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Deal>().AsReadOnly());

        var handler = new GetPipelineKanbanHandler(mockDealRepo.Object, mockPipelineRepo.Object);

        // Act
        var result = await handler.Handle(new GetPipelineKanbanQuery(_tenantId, _pipelineId), CancellationToken.None);

        // Assert
        result.Stages.Should().BeEmpty();
    }
}
