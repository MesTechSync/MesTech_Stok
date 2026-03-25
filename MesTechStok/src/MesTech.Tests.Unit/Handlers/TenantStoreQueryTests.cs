using FluentAssertions;
using MesTech.Application.DTOs.Dashboard;
using MesTech.Application.Features.Accounting.Queries.GetMonthlySummary;
using MesTech.Application.Features.Billing.Queries.GetSubscriptionPlans;
using MesTech.Application.Features.Billing.Queries.GetTenantSubscription;
using MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;
using MesTech.Application.Features.Stores.Queries.GetStoreCredential;
using MesTech.Application.Features.System.Users;
using MesTech.Application.Features.Tenant.Queries.GetTenant;
using MesTech.Application.Features.Tenant.Queries.GetTenants;
using MesTech.Application.Interfaces;
using MesTech.Application.Queries.GetStoresByTenant;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Tenant, Store, User, Subscription, and Dashboard query handler tests.
/// Covers: GetTenants, GetTenant, GetTenantSubscription, GetStoresByTenant,
/// GetStoreCredential, GetUsers, GetSubscriptionPlans, GetDashboardSummary, GetMonthlySummary.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "TenantStoreQueries")]
[Trait("Phase", "Dalga15")]
public class TenantStoreQueryTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly CancellationToken CT = CancellationToken.None;

    // ── GetTenantsHandler ──

    [Fact]
    public async Task GetTenantsHandler_EmptyResult_ReturnsEmptyList()
    {
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetAllAsync(CT))
            .ReturnsAsync(new List<MesTech.Domain.Entities.Tenant>());

        var sut = new GetTenantsHandler(repo.Object);
        var result = await sut.Handle(new GetTenantsQuery(), CT);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    // ── GetTenantHandler ──

    [Fact]
    public async Task GetTenantHandler_NotFound_ReturnsNull()
    {
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), CT))
            .ReturnsAsync((MesTech.Domain.Entities.Tenant?)null);

        var sut = new GetTenantHandler(repo.Object);
        var result = await sut.Handle(new GetTenantQuery(Guid.NewGuid()), CT);

        result.Should().BeNull();
    }

    // ── GetTenantSubscriptionHandler ──

    [Fact]
    public async Task GetTenantSubscriptionHandler_NullRequest_Throws()
    {
        var sut = new GetTenantSubscriptionHandler(
            new Mock<ITenantSubscriptionRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetTenantSubscriptionHandler_NotFound_ReturnsNull()
    {
        var repo = new Mock<ITenantSubscriptionRepository>();
        repo.Setup(r => r.GetActiveByTenantIdAsync(It.IsAny<Guid>(), CT))
            .ReturnsAsync((MesTech.Domain.Entities.Billing.TenantSubscription?)null);

        var sut = new GetTenantSubscriptionHandler(repo.Object);
        var result = await sut.Handle(new GetTenantSubscriptionQuery(TenantId), CT);

        result.Should().BeNull();
    }

    // ── GetStoresByTenantHandler ──

    [Fact]
    public async Task GetStoresByTenantHandler_NullRequest_Throws()
    {
        var sut = new GetStoresByTenantHandler(
            new Mock<IStoreRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetStoresByTenantHandler_EmptyResult_ReturnsEmptyList()
    {
        var repo = new Mock<IStoreRepository>();
        repo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), CT))
            .ReturnsAsync(new List<MesTech.Domain.Entities.Store>());

        var sut = new GetStoresByTenantHandler(repo.Object);
        var result = await sut.Handle(new GetStoresByTenantQuery(TenantId), CT);

        result.Should().BeEmpty();
    }

    // ── GetStoreCredentialHandler ──

    [Fact]
    public async Task GetStoreCredentialHandler_NullRequest_Throws()
    {
        var sut = new GetStoreCredentialHandler(
            new Mock<IStoreRepository>().Object,
            new Mock<IStoreCredentialRepository>().Object,
            new Mock<ICredentialEncryptionService>().Object,
            new Mock<ILogger<GetStoreCredentialHandler>>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetStoreCredentialHandler_StoreNotFound_ReturnsNull()
    {
        var storeRepo = new Mock<IStoreRepository>();
        storeRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), CT))
            .ReturnsAsync((MesTech.Domain.Entities.Store?)null);

        var sut = new GetStoreCredentialHandler(
            storeRepo.Object,
            new Mock<IStoreCredentialRepository>().Object,
            new Mock<ICredentialEncryptionService>().Object,
            new Mock<ILogger<GetStoreCredentialHandler>>().Object);

        var result = await sut.Handle(new GetStoreCredentialQuery(Guid.NewGuid()), CT);
        result.Should().BeNull();
    }

    // ── GetUsersHandler ──

    [Fact]
    public async Task GetUsersHandler_EmptyResult_ReturnsEmptyList()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAllAsync(CT))
            .ReturnsAsync(new List<MesTech.Domain.Entities.User>());

        var sut = new GetUsersHandler(repo.Object, new Mock<ILogger<GetUsersHandler>>().Object);
        var result = await sut.Handle(new GetUsersQuery(), CT);

        result.Should().BeEmpty();
    }

    // ── GetSubscriptionPlansHandler ──

    [Fact]
    public async Task GetSubscriptionPlansHandler_NullRequest_Throws()
    {
        var sut = new GetSubscriptionPlansHandler(
            new Mock<ISubscriptionPlanRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetSubscriptionPlansHandler_EmptyResult_ReturnsEmptyList()
    {
        var repo = new Mock<ISubscriptionPlanRepository>();
        repo.Setup(r => r.GetActiveAsync(CT))
            .ReturnsAsync(new List<MesTech.Domain.Entities.Billing.SubscriptionPlan>());

        var sut = new GetSubscriptionPlansHandler(repo.Object);
        var result = await sut.Handle(new GetSubscriptionPlansQuery(), CT);

        result.Should().BeEmpty();
    }

    // ── GetDashboardSummaryQueryHandler ──

    [Fact]
    public async Task GetDashboardSummaryQueryHandler_NullRequest_Throws()
    {
        var sut = new GetDashboardSummaryQueryHandler(
            new Mock<IDashboardSummaryRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }

    [Fact]
    public async Task GetDashboardSummaryQueryHandler_ValidRequest_DelegatesToRepo()
    {
        var expectedDto = new DashboardSummaryDto();
        var repo = new Mock<IDashboardSummaryRepository>();
        repo.Setup(r => r.GetSummaryAsync(TenantId, CT))
            .ReturnsAsync(expectedDto);

        var sut = new GetDashboardSummaryQueryHandler(repo.Object);
        var result = await sut.Handle(new GetDashboardSummaryQuery(TenantId), CT);

        result.Should().BeSameAs(expectedDto);
    }

    // ── GetMonthlySummaryHandler ──

    [Fact]
    public async Task GetMonthlySummaryHandler_NullRequest_Throws()
    {
        var sut = new GetMonthlySummaryHandler(
            new Mock<IOrderRepository>().Object,
            new Mock<IExpenseRepository>().Object,
            new Mock<IIncomeRepository>().Object);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.Handle(null!, CT));
    }
}
