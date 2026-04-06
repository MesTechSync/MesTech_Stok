using FluentAssertions;
using MesTech.Application.Queries.GetCustomersPaged;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetCustomersPagedHandlerTests
{
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly GetCustomersPagedHandler _sut;

    public GetCustomersPagedHandlerTests()
    {
        _sut = new GetCustomersPagedHandler(_customerRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedCustomers()
    {
        var customers = Enumerable.Range(1, 5).Select(i => new Customer
        {
            Id = Guid.NewGuid(),
            Name = $"Müşteri {i}",
            Code = $"MUS-{i:D3}",
            IsActive = true
        }).ToList();

        _customerRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(customers.AsReadOnly());

        var query = new GetCustomersPagedQuery(Page: 1, PageSize: 3);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithSearch_FiltersResults()
    {
        var customers = new List<Customer>
        {
            new() { Id = Guid.NewGuid(), Name = "Ahmet Ltd.", Code = "AHM-001" },
            new() { Id = Guid.NewGuid(), Name = "Mehmet A.Ş.", Code = "MHM-001" },
            new() { Id = Guid.NewGuid(), Name = "Ahmet Ticaret", Code = "AHM-002" }
        };

        _customerRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(customers.AsReadOnly());

        var query = new GetCustomersPagedQuery(SearchTerm: "Ahmet");
        var result = await _sut.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Items.Should().AllSatisfy(c => c.Name.Should().Contain("Ahmet"));
    }

    [Fact]
    public async Task Handle_Page2_ReturnsCorrectSlice()
    {
        var customers = Enumerable.Range(1, 10).Select(i => new Customer
        {
            Id = Guid.NewGuid(), Name = $"Müşteri {i}", Code = $"MUS-{i:D3}"
        }).ToList();

        _customerRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(customers.AsReadOnly());

        var query = new GetCustomersPagedQuery(Page: 2, PageSize: 3);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(10);
        result.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("Müşteri 4");
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
