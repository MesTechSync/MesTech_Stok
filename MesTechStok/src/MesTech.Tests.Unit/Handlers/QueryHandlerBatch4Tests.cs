using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetFixedExpenseById;
using MesTech.Application.Features.Accounting.Queries.GetPenaltyRecordById;
using MesTech.Application.Features.Accounting.Queries.GetSalaryRecordById;
using MesTech.Application.Features.Accounting.Queries.GetTaxRecordById;
using MesTech.Application.Features.Logging.Queries.GetLogCount;
using MesTech.Application.Features.Onboarding.Queries.GetOnboardingProgress;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Application.Queries.GetCategoriesPaged;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Onboarding;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Batch 4 — GetLogCount, GetCategoriesPaged, GetFixedExpenseById,
/// GetPenaltyRecordById, GetSalaryRecordById, GetTaxRecordById, GetOnboardingProgress
/// </summary>
[Trait("Category", "Unit")]
public class QueryHandlerBatch4Tests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ═══════════════════════════════════════════════════════════
    // GetLogCountHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetLogCount_ReturnsCount()
    {
        var repo = new Mock<ILogEntryRepository>();
        repo.Setup(r => r.GetCountAsync(_tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var sut = new GetLogCountHandler(repo.Object);
        var result = await sut.Handle(new GetLogCountQuery(_tenantId), CancellationToken.None);

        result.Should().Be(42);
    }

    [Fact]
    public async Task GetLogCount_WithCategory_PassesCategory()
    {
        var repo = new Mock<ILogEntryRepository>();
        repo.Setup(r => r.GetCountAsync(_tenantId, "Stock", It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        var sut = new GetLogCountHandler(repo.Object);
        var result = await sut.Handle(new GetLogCountQuery(_tenantId, "Stock"), CancellationToken.None);

        result.Should().Be(10);
    }

    // ═══════════════════════════════════════════════════════════
    // GetCategoriesPagedHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetCategoriesPaged_EmptyRepo_ReturnsEmptyResult()
    {
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>());

        var sut = new GetCategoriesPagedHandler(repo.Object);
        var result = await sut.Handle(new GetCategoriesPagedQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetCategoriesPaged_WithSearch_FiltersResults()
    {
        var cat1 = Category.Create(_tenantId, "Elektronik", "ELEC");
        var cat2 = Category.Create(_tenantId, "Giyim", "WEAR");
        var repo = new Mock<ICategoryRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { cat1, cat2 });

        var sut = new GetCategoriesPagedHandler(repo.Object);
        var result = await sut.Handle(
            new GetCategoriesPagedQuery(SearchTerm: "Elek"), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    // ═══════════════════════════════════════════════════════════
    // GetFixedExpenseByIdHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetFixedExpenseById_NotFound_ReturnsNull()
    {
        var repo = new Mock<IFixedExpenseRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FixedExpense?)null);

        var sut = new GetFixedExpenseByIdHandler(repo.Object);
        var result = await sut.Handle(
            new GetFixedExpenseByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════
    // GetPenaltyRecordByIdHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetPenaltyRecordById_NotFound_ReturnsNull()
    {
        var repo = new Mock<IPenaltyRecordRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PenaltyRecord?)null);

        var sut = new GetPenaltyRecordByIdHandler(repo.Object);
        var result = await sut.Handle(
            new GetPenaltyRecordByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════
    // GetSalaryRecordByIdHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetSalaryRecordById_NotFound_ReturnsNull()
    {
        var repo = new Mock<ISalaryRecordRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SalaryRecord?)null);

        var sut = new GetSalaryRecordByIdHandler(repo.Object);
        var result = await sut.Handle(
            new GetSalaryRecordByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════
    // GetTaxRecordByIdHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetTaxRecordById_NotFound_ReturnsNull()
    {
        var repo = new Mock<ITaxRecordRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaxRecord?)null);

        var sut = new GetTaxRecordByIdHandler(repo.Object);
        var result = await sut.Handle(
            new GetTaxRecordByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    // ═══════════════════════════════════════════════════════════
    // GetOnboardingProgressHandler
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task GetOnboardingProgress_NotFound_ReturnsNull()
    {
        var repo = new Mock<IOnboardingProgressRepository>();
        repo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingProgress?)null);

        var sut = new GetOnboardingProgressHandler(repo.Object);
        var result = await sut.Handle(
            new GetOnboardingProgressQuery(_tenantId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetOnboardingProgress_Found_MapsToDto()
    {
        var progress = OnboardingProgress.Start(_tenantId);
        var repo = new Mock<IOnboardingProgressRepository>();
        repo.Setup(r => r.GetByTenantIdAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);

        var sut = new GetOnboardingProgressHandler(repo.Object);
        var result = await sut.Handle(
            new GetOnboardingProgressQuery(_tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsCompleted.Should().BeFalse();
    }
}
