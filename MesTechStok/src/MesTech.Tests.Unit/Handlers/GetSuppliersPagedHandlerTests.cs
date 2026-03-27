using FluentAssertions;
using MesTech.Application.Queries.GetSuppliersPaged;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetSuppliersPagedHandlerTests
{
    private readonly Mock<ISupplierRepository> _supplierRepoMock = new();
    private readonly GetSuppliersPagedHandler _sut;

    public GetSuppliersPagedHandlerTests()
    {
        _sut = new GetSuppliersPagedHandler(_supplierRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedSuppliers()
    {
        var suppliers = Enumerable.Range(1, 5).Select(i => new Supplier
        {
            Id = Guid.NewGuid(),
            Name = $"Tedarikçi {i}",
            Code = $"TDK-{i:D3}",
            IsActive = true
        }).ToList();

        _supplierRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(suppliers.AsReadOnly());

        var query = new GetSuppliersPagedQuery(Page: 1, PageSize: 3);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(5);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithSearch_FiltersResults()
    {
        var suppliers = new List<Supplier>
        {
            new Supplier { Id = Guid.NewGuid(), Name = "ABC Tedarik", Code = "ABC-001" },
            new Supplier { Id = Guid.NewGuid(), Name = "XYZ Üretim", Code = "XYZ-001" },
            new Supplier { Id = Guid.NewGuid(), Name = "ABC Lojistik", Code = "ABC-002" }
        };

        _supplierRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(suppliers.AsReadOnly());

        var query = new GetSuppliersPagedQuery(SearchTerm: "ABC");
        var result = await _sut.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_EmptyList_ReturnsZero()
    {
        _supplierRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Supplier>().AsReadOnly());

        var query = new GetSuppliersPagedQuery();
        var result = await _sut.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
