using FluentAssertions;
using MesTech.Application.Features.Product.Queries.GetProducts;
using MesTech.Application.Features.System.Users;
using MesTech.Application.Features.Tenant.Queries.GetTenant;
using MesTech.Application.Queries.GetCategoriesPaged;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

/// <summary>
/// DEV5 Batch 7: Query handler testleri — GetProducts, GetCategoriesPaged, GetTenant, GetUsers.
/// </summary>

#region GetProducts

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetProductsHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();

    [Fact]
    public async Task Handle_NoProducts_ShouldReturnEmptyPage()
    {
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        var handler = new GetProductsHandler(_productRepo.Object);
        var result = await handler.Handle(new GetProductsQuery(Guid.NewGuid()), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithProducts_ShouldReturnPaged()
    {
        var products = Enumerable.Range(1, 10).Select(i =>
            FakeData.CreateProduct(sku: $"SKU-{i}", stock: i * 10)).ToList();
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var handler = new GetProductsHandler(_productRepo.Object);
        var result = await handler.Handle(
            new GetProductsQuery(Guid.NewGuid(), PageSize: 5), CancellationToken.None);

        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(10);
    }

    [Fact]
    public async Task Handle_SearchTerm_ShouldUseSearchAsync()
    {
        _productRepo.Setup(r => r.SearchAsync("laptop", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { FakeData.CreateProduct(sku: "LAPTOP-1") });

        var handler = new GetProductsHandler(_productRepo.Object);
        var result = await handler.Handle(
            new GetProductsQuery(Guid.NewGuid(), SearchTerm: "laptop"), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        _productRepo.Verify(r => r.SearchAsync("laptop", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LowStockFilter_ShouldFilterCorrectly()
    {
        var products = new List<Product>
        {
            FakeData.CreateProduct(stock: 100, minimumStock: 10),
            FakeData.CreateProduct(stock: 3, minimumStock: 10),  // low stock
        };
        _productRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var handler = new GetProductsHandler(_productRepo.Object);
        var result = await handler.Handle(
            new GetProductsQuery(Guid.NewGuid(), LowStockOnly: true), CancellationToken.None);

        result.TotalCount.Should().Be(1);
    }
}

#endregion

#region GetCategoriesPaged

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetCategoriesPagedHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepo = new();

    [Fact]
    public async Task Handle_NoCategories_ShouldReturnEmpty()
    {
        _categoryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        var handler = new GetCategoriesPagedHandler(_categoryRepo.Object);
        var result = await handler.Handle(new GetCategoriesPagedQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithSearch_ShouldFilterByName()
    {
        var categories = new List<Category>
        {
            new() { Name = "Elektronik", Code = "ELK", TenantId = Guid.NewGuid() },
            new() { Name = "Gıda", Code = "GDA", TenantId = Guid.NewGuid() },
        };
        _categoryRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var handler = new GetCategoriesPagedHandler(_categoryRepo.Object);
        var result = await handler.Handle(new GetCategoriesPagedQuery("Elek"), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Elektronik");
    }
}

#endregion

#region GetTenant

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetTenantHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepo = new();

    [Fact]
    public async Task Handle_TenantExists_ShouldReturnDto()
    {
        var tenant = new MesTech.Domain.Entities.Tenant { Name = "MesTech", TaxNumber = "123", IsActive = true };
        _tenantRepo.Setup(r => r.GetByIdAsync(tenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var handler = new GetTenantHandler(_tenantRepo.Object);
        var result = await handler.Handle(new GetTenantQuery(tenant.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Name.Should().Be("MesTech");
    }

    [Fact]
    public async Task Handle_TenantNotFound_ShouldReturnNull()
    {
        _tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MesTech.Domain.Entities.Tenant?)null);

        var handler = new GetTenantHandler(_tenantRepo.Object);
        var result = await handler.Handle(new GetTenantQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }
}

#endregion

#region GetUsers

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetUsersHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ILogger<GetUsersHandler>> _logger = new();

    [Fact]
    public async Task Handle_NoUsers_ShouldReturnEmpty()
    {
        _userRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User>());

        var handler = new GetUsersHandler(_userRepo.Object, _logger.Object);
        var result = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithTenantFilter_ShouldFilterByTenant()
    {
        var tenantId = Guid.NewGuid();
        var users = new List<User>
        {
            new() { TenantId = tenantId, Username = "admin", Email = "a@b.com", IsActive = true },
            new() { TenantId = Guid.NewGuid(), Username = "other", Email = "o@b.com", IsActive = true },
        };
        _userRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var handler = new GetUsersHandler(_userRepo.Object, _logger.Object);
        var result = await handler.Handle(new GetUsersQuery(tenantId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Username.Should().Be("admin");
    }
}

#endregion
