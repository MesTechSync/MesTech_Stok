using FluentAssertions;
using MesTech.Application.Features.Dashboard.Queries.GetLowStockAlerts;
using MesTech.Application.Features.Hr.Queries.GetDepartments;
using MesTech.Application.Features.Logging.Queries.GetLogs;
using MesTech.Application.Features.Notifications.Queries.GetNotifications;
using MesTech.Application.Features.System.Users;
using MesTech.Application.Features.Tasks.Queries.GetProjects;
using MesTech.Application.Features.Tenant.Queries.GetTenants;
using MesTech.Application.Interfaces;
using MesTech.Application.Queries.GetCategories;
using MesTech.Application.Queries.GetWarehouses;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Hr;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Batch 1 — 10 testsiz Get*QueryHandler kapanisi.
/// Her handler icin: bos liste, dolu liste, filtre senaryolari.
/// </summary>
[Trait("Category", "Unit")]
public class QueryHandlerBatch1Tests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════════════════════════════════════════════════════════
    // GetWarehousesHandler
    // ══════════════���════════════════════════════════════════════

    [Fact]
    public async Task GetWarehouses_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IWarehouseRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Warehouse>());

        var sut = new GetWarehousesHandler(repo.Object);
        var result = await sut.Handle(new GetWarehousesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWarehouses_WithData_MapsToDto()
    {
        var wh = new Warehouse
        {
            Id = Guid.NewGuid(), Name = "Ana Depo", Code = "WH01",
            Type = "MAIN", IsActive = true, HasClimateControl = true
        };
        var repo = new Mock<IWarehouseRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Warehouse> { wh });

        var sut = new GetWarehousesHandler(repo.Object);
        var result = await sut.Handle(new GetWarehousesQuery(ActiveOnly: false), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Ana Depo");
        result[0].Code.Should().Be("WH01");
        result[0].HasClimateControl.Should().BeTrue();
    }

    [Fact]
    public async Task GetWarehouses_ActiveOnly_FiltersInactive()
    {
        var active = new Warehouse { Id = Guid.NewGuid(), Name = "Aktif", IsActive = true };
        var inactive = new Warehouse { Id = Guid.NewGuid(), Name = "Pasif", IsActive = false };
        var repo = new Mock<IWarehouseRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Warehouse> { active, inactive });

        var sut = new GetWarehousesHandler(repo.Object);
        var result = await sut.Handle(new GetWarehousesQuery(ActiveOnly: true), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Aktif");
    }

    // ════════════════════════════════════════���══════════════════
    // GetCategoriesHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCategories_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        var sut = new GetCategoriesHandler(repo.Object);
        var result = await sut.Handle(new GetCategoriesQuery(ActiveOnly: true), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategories_WithData_MapsToDto()
    {
        var cat = Category.Create(_tenantId, "Elektronik", "ELEC");
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { cat });

        var sut = new GetCategoriesHandler(repo.Object);
        var result = await sut.Handle(new GetCategoriesQuery(ActiveOnly: true), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Elektronik");
        result[0].Code.Should().Be("ELEC");
        result[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetCategories_AllIncludingInactive_UsesGetAllAsync()
    {
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        var sut = new GetCategoriesHandler(repo.Object);
        await sut.Handle(new GetCategoriesQuery(ActiveOnly: false), CancellationToken.None);

        repo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ════════════════���═════════════════════════��════════════════
    // GetDepartmentsHandler
    // ═════════════════��═════════════════════════════════════════

    [Fact]
    public async Task GetDepartments_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IDepartmentRepository>();
        repo.Setup(r => r.GetByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department>());

        var sut = new GetDepartmentsHandler(repo.Object);
        var result = await sut.Handle(new GetDepartmentsQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDepartments_WithData_MapsToDto()
    {
        var dept = Department.Create(_tenantId, "IT");
        var repo = new Mock<IDepartmentRepository>();
        repo.Setup(r => r.GetByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department> { dept });

        var sut = new GetDepartmentsHandler(repo.Object);
        var result = await sut.Handle(new GetDepartmentsQuery(_tenantId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("IT");
        result[0].IsActive.Should().BeTrue();
    }

    // ════════════════════════════════════════���══════════════════
    // GetProjectsHandler
    // ═════════���════════════════════════════════════════════════��

    [Fact]
    public async Task GetProjects_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IProjectRepository>();
        repo.Setup(r => r.GetByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project>());

        var sut = new GetProjectsHandler(repo.Object);
        var result = await sut.Handle(new GetProjectsQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjects_WithData_MapsToDto()
    {
        var project = Project.Create(_tenantId, "MesTech v2");
        var repo = new Mock<IProjectRepository>();
        repo.Setup(r => r.GetByTenantAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Project> { project });

        var sut = new GetProjectsHandler(repo.Object);
        var result = await sut.Handle(new GetProjectsQuery(_tenantId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("MesTech v2");
    }

    // ══════════════════��═══════════════════════════════════��════
    // GetTenantsHandler
    // ═══════════════════���═════════════════════════���═════════════

    [Fact]
    public async Task GetTenants_EmptyRepo_ReturnsEmptyResult()
    {
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tenant>());

        var sut = new GetTenantsHandler(repo.Object);
        var result = await sut.Handle(new GetTenantsQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTenants_WithData_MapsAndPaginates()
    {
        var tenants = Enumerable.Range(1, 5).Select(i => new Tenant
        {
            Id = Guid.NewGuid(),
            Name = $"Firma {i}",
            TaxNumber = $"VKN{i:D10}",
            IsActive = true,
            Stores = new List<Store>(),
            Users = new List<User>()
        }).ToList();

        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenants);

        var sut = new GetTenantsHandler(repo.Object);
        var result = await sut.Handle(new GetTenantsQuery(Page: 1, PageSize: 3), CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(3);
        result.Page.Should().Be(1);
    }

    // ═══════════════════════════════════════════════════════════
    // GetUsersHandler
    // ══════════════════��═════════════════════════���══════════════

    [Fact]
    public async Task GetUsers_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());
        var logger = new Mock<ILogger<GetUsersHandler>>();

        var sut = new GetUsersHandler(repo.Object, logger.Object);
        var result = await sut.Handle(new GetUsersQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUsers_WithTenantFilter_FiltersCorrectly()
    {
        var matchUser = new User
        {
            Id = Guid.NewGuid(), Username = "admin", FirstName = "Admin", LastName = "User",
            Email = "admin@test.com", IsActive = true, TenantId = _tenantId
        };
        var otherUser = new User
        {
            Id = Guid.NewGuid(), Username = "other", FirstName = "Other", LastName = "User",
            Email = "other@test.com", IsActive = true, TenantId = Guid.NewGuid()
        };
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { matchUser, otherUser });
        var logger = new Mock<ILogger<GetUsersHandler>>();

        var sut = new GetUsersHandler(repo.Object, logger.Object);
        var result = await sut.Handle(new GetUsersQuery(TenantId: _tenantId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Username.Should().Be("admin");
    }

    // ══════════════════════════════════════════════════��════════
    // GetLogsHandler
    // ═════════════════���════════════════════════════════��════════

    [Fact]
    public async Task GetLogs_CallsRepoWithCorrectParameters()
    {
        var repo = new Mock<ILogEntryRepository>();
        repo.Setup(r => r.GetPagedAsync(
                _tenantId, 1, 50, null, null, null, null, null, null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LogEntry>());

        var sut = new GetLogsHandler(repo.Object);
        var result = await sut.Handle(new GetLogsQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
        repo.Verify(r => r.GetPagedAsync(
            _tenantId, 1, 50, null, null, null, null, null, null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ═══════════════���══════════════════════��════════════════════
    // GetNotificationsHandler
    // ═══════════════��═══════════════════════════════════════════

    [Fact]
    public async Task GetNotifications_EmptyRepo_ReturnsEmptyResult()
    {
        var repo = new Mock<INotificationLogRepository>();
        repo.Setup(r => r.GetPagedAsync(_tenantId, 1, 20, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<NotificationLog>(), 0));

        var sut = new GetNotificationsHandler(repo.Object);
        var result = await sut.Handle(
            new GetNotificationsQuery(_tenantId), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ══════════════════════════════════════════���════════════════
    // GetLowStockAlertsHandler
    // ═════════════���═════════════════════════════════════════════

    [Fact]
    public async Task GetLowStockAlerts_EmptyRepo_ReturnsEmptyList()
    {
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetLowStockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var sut = new GetLowStockAlertsHandler(repo.Object);
        var result = await sut.Handle(
            new GetLowStockAlertsQuery(_tenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLowStockAlerts_WithLowStockProducts_MapsCorrectly()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(), Name = "Test Urun", SKU = "TST001",
            MinimumStock = 10
        };
        product.SyncStock(2);
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetLowStockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var sut = new GetLowStockAlertsHandler(repo.Object);
        var result = await sut.Handle(
            new GetLowStockAlertsQuery(_tenantId, Count: 5), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].ProductName.Should().Be("Test Urun");
        result[0].CurrentStock.Should().Be(2);
        result[0].MinimumStock.Should().Be(10);
        result[0].Deficit.Should().Be(8);
        result[0].Severity.Should().Be("Warning");
    }

    [Fact]
    public async Task GetLowStockAlerts_ZeroStock_SeverityCritical()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(), Name = "Kritik", SKU = "KRT001",
            MinimumStock = 5
        };
        var repo = new Mock<IProductRepository>();
        repo.Setup(r => r.GetLowStockAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        var sut = new GetLowStockAlertsHandler(repo.Object);
        var result = await sut.Handle(
            new GetLowStockAlertsQuery(_tenantId), CancellationToken.None);

        result[0].Severity.Should().Be("Critical");
    }
}
