using FluentAssertions;
using MediatR;
using MesTech.Application.DTOs.Crm;
using PlatformMessage = MesTech.Domain.Entities.PlatformMessage;
using MesTech.Application.Features.Crm.Commands.CreateDeal;
using MesTech.Application.Features.Crm.Commands.CreateLead;
using MesTech.Application.Features.Crm.Commands.LoseDeal;
using MesTech.Application.Features.Crm.Commands.ReplyToMessage;
using MesTech.Application.Features.Crm.Commands.WinDeal;
using MesTech.Application.Features.Crm.Queries.GetCrmDashboard;
using MesTech.Application.Features.Crm.Queries.GetCustomersCrm;
using MesTech.Application.Features.Crm.Queries.GetSuppliersCrm;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

// ═══════════════════════════════════════════════════════════════
// DEV 5 — CRM Extra Handler Tests
// CreateDeal, CreateLead, WinDeal, LoseDeal,
// GetCrmDashboard, GetCustomersCrm, GetSuppliersCrm, ReplyToMessage
// ═══════════════════════════════════════════════════════════════

#region CreateDealHandler

[Trait("Category", "Unit")]
[Trait("Feature", "CRM")]
public class CreateDealHandlerTests2
{
    private readonly Mock<ICrmDealRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateDealHandler _sut;

    public CreateDealHandlerTests2()
    {
        _sut = new CreateDealHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsGuid()
    {
        var cmd = new CreateDealCommand(
            Guid.NewGuid(), "Test Deal", Guid.NewGuid(), Guid.NewGuid(), 5000m);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Deal>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullCommand_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region CreateLeadHandler

[Trait("Category", "Unit")]
[Trait("Feature", "CRM")]
public class CreateLeadHandlerTests2
{
    private readonly Mock<ICrmLeadRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateLeadHandler _sut;

    public CreateLeadHandlerTests2()
    {
        _sut = new CreateLeadHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsGuid()
    {
        var cmd = new CreateLeadCommand(
            Guid.NewGuid(), "John Doe", LeadSource.Web, "john@test.com");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Lead>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullCommand_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region WinDealHandler

[Trait("Category", "Unit")]
[Trait("Feature", "CRM")]
public class WinDealHandlerTests
{
    private readonly Mock<ICrmDealRepository> _dealsMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly WinDealHandler _sut;

    public WinDealHandlerTests()
    {
        _sut = new WinDealHandler(_dealsMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_DealNotFound_ThrowsInvalidOperationException()
    {
        _dealsMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Deal?)null);

        var cmd = new WinDealCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DealExists_MarksAsWonAndSaves()
    {
        var deal = Deal.Create(Guid.NewGuid(), "Won Deal", Guid.NewGuid(), Guid.NewGuid(), 1000m);
        _dealsMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        var cmd = new WinDealCommand(deal.Id, Guid.NewGuid());
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region LoseDealHandler

[Trait("Category", "Unit")]
[Trait("Feature", "CRM")]
public class LoseDealHandlerTests
{
    private readonly Mock<ICrmDealRepository> _dealsMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly LoseDealHandler _sut;

    public LoseDealHandlerTests()
    {
        _sut = new LoseDealHandler(_dealsMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_DealNotFound_ThrowsInvalidOperationException()
    {
        _dealsMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Deal?)null);

        var cmd = new LoseDealCommand(Guid.NewGuid(), "Price too high");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DealExists_MarksAsLostAndSaves()
    {
        var deal = Deal.Create(Guid.NewGuid(), "Lost Deal", Guid.NewGuid(), Guid.NewGuid(), 2000m);
        _dealsMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deal);

        var cmd = new LoseDealCommand(deal.Id, "Budget exceeded");
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

// GetCrmDashboardHandlerTests — moved to standalone file
// GetCustomersCrmHandlerTests — moved to standalone file

#region GetSuppliersCrmHandler

[Trait("Category", "Unit")]
[Trait("Feature", "CRM")]
public class GetSuppliersCrmHandlerTests
{
    private readonly Mock<ICrmDashboardQueryService> _queryServiceMock = new();
    private readonly GetSuppliersCrmHandler _sut;

    public GetSuppliersCrmHandlerTests()
    {
        _sut = new GetSuppliersCrmHandler(_queryServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsSuppliersResult()
    {
        var items = new List<SupplierCrmDto>().AsReadOnly();
        _queryServiceMock.Setup(s => s.GetSuppliersPagedAsync(
                It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
                It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((items, 0));

        var query = new GetSuppliersCrmQuery(Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NullQuery_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region ReplyToMessageHandler

[Trait("Category", "Unit")]
[Trait("Feature", "CRM")]
public class ReplyToMessageHandlerTests2
{
    private readonly Mock<IPlatformMessageRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly ReplyToMessageHandler _sut;

    public ReplyToMessageHandlerTests2()
    {
        _sut = new ReplyToMessageHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_MessageNotFound_ThrowsInvalidOperationException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlatformMessage?)null);

        var cmd = new ReplyToMessageCommand(Guid.NewGuid(), "Reply text", "admin");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.Handle(cmd, CancellationToken.None));
    }
}

#endregion
