using FluentAssertions;
using MesTech.Application.Features.Crm.Queries.GetLeads;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "CrmLeadQueries")]
public class LeadQueryHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    [Fact]
    public async Task Handle_WithLeads_ShouldReturnMappedDtos()
    {
        // Arrange
        var lead1 = Lead.Create(_tenantId, "Ali Yilmaz", LeadSource.Web, "ali@test.com");
        var lead2 = Lead.Create(_tenantId, "Veli Kaya", LeadSource.WhatsApp, phone: "+905551234567");
        var leads = new List<Lead> { lead1, lead2 }.AsReadOnly();

        var mockRepo = new Mock<ICrmLeadRepository>();
        mockRepo.Setup(r => r.GetPagedAsync(_tenantId, null, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((leads, 2));

        var handler = new GetLeadsHandler(mockRepo.Object);
        var query = new GetLeadsQuery(_tenantId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items[0].FullName.Should().Be("Ali Yilmaz");
        result.Items[0].Source.Should().Be("Web");
        result.Items[1].FullName.Should().Be("Veli Kaya");
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldPassToRepo()
    {
        // Arrange
        var mockRepo = new Mock<ICrmLeadRepository>();
        mockRepo.Setup(r => r.GetPagedAsync(
                _tenantId, LeadStatus.Contacted, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Lead>().AsReadOnly(), 0));

        var handler = new GetLeadsHandler(mockRepo.Object);
        var query = new GetLeadsQuery(_tenantId, Status: LeadStatus.Contacted);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        mockRepo.Verify(r => r.GetPagedAsync(
            _tenantId, LeadStatus.Contacted, null, 1, 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var handler = new GetLeadsHandler(Mock.Of<ICrmLeadRepository>());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
