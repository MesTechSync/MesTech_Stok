using FluentAssertions;
using MesTech.Application.Commands.CreateCustomer;
using MesTech.Application.Commands.UpdateCustomer;
using MesTech.Application.Queries.GetCustomersPaged;
using MesTech.Application.Features.Crm.Commands.CreateLead;
using MesTech.Application.Features.Crm.Commands.CreateDeal;
using MesTech.Application.Features.Crm.Commands.WinDeal;
using MesTech.Application.Features.Crm.Commands.LoseDeal;
using MesTech.Application.Features.Crm.Queries.GetLeads;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

/// <summary>
/// CRM Handler testleri — Customer, Lead, Deal null-guard + temel kontroller.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
[Trait("Group", "Handler")]
public class CrmHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();

    public CrmHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    // ═══ CreateCustomer ═══

    [Fact]
    public async Task CreateCustomer_NullRequest_Throws()
    {
        var repo = new Mock<ICustomerRepository>();
        var handler = new CreateCustomerHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ UpdateCustomer ═══

    [Fact]
    public async Task UpdateCustomer_NullRequest_Throws()
    {
        var repo = new Mock<ICustomerRepository>();
        var handler = new UpdateCustomerHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetCustomersPaged ═══

    [Fact]
    public async Task GetCustomersPaged_NullRequest_Throws()
    {
        var repo = new Mock<ICustomerRepository>();
        var handler = new GetCustomersPagedHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ CreateLead ═══

    [Fact]
    public async Task CreateLead_NullRequest_Throws()
    {
        var repo = new Mock<ICrmLeadRepository>();
        var handler = new CreateLeadHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ GetLeads ═══

    [Fact]
    public async Task GetLeads_NullRequest_Throws()
    {
        var repo = new Mock<ICrmLeadRepository>();
        var handler = new GetLeadsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ CreateDeal ═══

    [Fact]
    public async Task CreateDeal_NullRequest_Throws()
    {
        var repo = new Mock<ICrmDealRepository>();
        var handler = new CreateDealHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ WinDeal ═══

    [Fact]
    public async Task WinDeal_NullRequest_Throws()
    {
        var repo = new Mock<ICrmDealRepository>();
        var handler = new WinDealHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }

    // ═══ LoseDeal ═══

    [Fact]
    public async Task LoseDeal_NullRequest_Throws()
    {
        var repo = new Mock<ICrmDealRepository>();
        var handler = new LoseDealHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }
}
