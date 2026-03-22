using FluentAssertions;
using MesTech.Application.DTOs.Crm;
using MesTech.Application.Features.Crm.Queries.GetCrmDashboard;
using MesTech.Application.Features.Crm.Queries.GetCustomersCrm;
using MesTech.Application.Features.Crm.Queries.GetSuppliersCrm;
using MesTech.Application.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Crm;

[Trait("Category", "Unit")]
[Trait("Feature", "CrmDashboardQueries")]
public class CrmDashboardQueryHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    // ── GetCrmDashboardHandler ──

    [Fact]
    public async Task GetCrmDashboard_ValidTenant_ShouldReturnDto()
    {
        // Arrange
        var expectedDto = new CrmDashboardDto
        {
            TotalCustomers = 100,
            ActiveCustomers = 80,
            VipCustomers = 10,
            TotalSuppliers = 20,
            TotalLeads = 50,
            OpenDeals = 15,
            PipelineValue = 250000m
        };
        var mockService = new Mock<ICrmDashboardQueryService>();
        mockService.Setup(s => s.GetDashboardAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        var handler = new GetCrmDashboardHandler(mockService.Object);

        // Act
        var result = await handler.Handle(new GetCrmDashboardQuery(_tenantId), CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expectedDto);
        result.TotalCustomers.Should().Be(100);
        result.PipelineValue.Should().Be(250000m);
    }

    [Fact]
    public async Task GetCrmDashboard_NullRequest_ShouldThrow()
    {
        var handler = new GetCrmDashboardHandler(Mock.Of<ICrmDashboardQueryService>());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetCrmDashboard_ServiceCalled_ShouldPassTenantId()
    {
        // Arrange
        var mockService = new Mock<ICrmDashboardQueryService>();
        mockService.Setup(s => s.GetDashboardAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrmDashboardDto());

        var handler = new GetCrmDashboardHandler(mockService.Object);
        await handler.Handle(new GetCrmDashboardQuery(_tenantId), CancellationToken.None);

        // Assert
        mockService.Verify(s => s.GetDashboardAsync(_tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── GetCustomersCrmHandler ──

    [Fact]
    public async Task GetCustomersCrm_ValidRequest_ShouldReturnResult()
    {
        // Arrange
        var items = new List<CustomerCrmDto>
        {
            new() { Name = "Customer 1" },
            new() { Name = "Customer 2" }
        }.AsReadOnly();

        var mockService = new Mock<ICrmDashboardQueryService>();
        mockService.Setup(s => s.GetCustomersPagedAsync(
                _tenantId, null, null, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 2));

        var handler = new GetCustomersCrmHandler(mockService.Object);
        var query = new GetCustomersCrmQuery(_tenantId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetCustomersCrm_WithFilters_ShouldPassFilters()
    {
        // Arrange
        var mockService = new Mock<ICrmDashboardQueryService>();
        mockService.Setup(s => s.GetCustomersPagedAsync(
                _tenantId, true, true, "search", 2, 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<CustomerCrmDto>().AsReadOnly(), 0));

        var handler = new GetCustomersCrmHandler(mockService.Object);
        var query = new GetCustomersCrmQuery(_tenantId, IsVip: true, IsActive: true, SearchTerm: "search", Page: 2, PageSize: 25);

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        mockService.Verify(s => s.GetCustomersPagedAsync(
            _tenantId, true, true, "search", 2, 25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCustomersCrm_NullRequest_ShouldThrow()
    {
        var handler = new GetCustomersCrmHandler(Mock.Of<ICrmDashboardQueryService>());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── GetSuppliersCrmHandler ──

    [Fact]
    public async Task GetSuppliersCrm_ValidRequest_ShouldReturnResult()
    {
        // Arrange
        var items = new List<SupplierCrmDto>
        {
            new() { Name = "Supplier A" }
        }.AsReadOnly();

        var mockService = new Mock<ICrmDashboardQueryService>();
        mockService.Setup(s => s.GetSuppliersPagedAsync(
                _tenantId, null, null, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 1));

        var handler = new GetSuppliersCrmHandler(mockService.Object);
        var query = new GetSuppliersCrmQuery(_tenantId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetSuppliersCrm_WithFilters_ShouldPassFilters()
    {
        // Arrange
        var mockService = new Mock<ICrmDashboardQueryService>();
        mockService.Setup(s => s.GetSuppliersPagedAsync(
                _tenantId, true, true, "test", 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<SupplierCrmDto>().AsReadOnly(), 0));

        var handler = new GetSuppliersCrmHandler(mockService.Object);
        var query = new GetSuppliersCrmQuery(_tenantId, IsActive: true, IsPreferred: true, SearchTerm: "test");

        // Act
        await handler.Handle(query, CancellationToken.None);

        // Assert
        mockService.Verify(s => s.GetSuppliersPagedAsync(
            _tenantId, true, true, "test", 1, 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSuppliersCrm_NullRequest_ShouldThrow()
    {
        var handler = new GetSuppliersCrmHandler(Mock.Of<ICrmDashboardQueryService>());
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
