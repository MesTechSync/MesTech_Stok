using FluentAssertions;
using MesTech.Application.DTOs.Crm;
using MesTech.Application.Features.Crm.Commands.ExportCustomers;
using MesTech.Application.Features.Crm.Commands.RedeemPoints;
using MesTech.Application.Features.Crm.Commands.SaveCrmSettings;
using MesTech.Application.Features.Crm.Queries.GetActiveCampaigns;
using MesTech.Application.Features.Crm.Queries.GetBitrix24Pipeline;
using MesTech.Application.Features.Crm.Queries.GetContactsPaged;
using MesTech.Application.Features.Crm.Queries.GetCrmSettings;
using MesTech.Application.Features.Crm.Queries.GetCustomerPoints;
using MesTech.Application.Features.Crm.Queries.GetDeals;
using MesTech.Application.Features.Crm.Queries.GetLeads;
using MesTech.Application.Features.Crm.Queries.GetLeadScore;
using MesTech.Application.Features.Crm.Queries.GetPipelineKanban;
using MesTech.Application.Features.Crm.Queries.GetPlatformMessages;
using MesTech.Application.Features.Crm.Queries.GetSuppliersCrm;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

// ═══════════════════════════════════════════════════════════════
// DEV 5 — CRM Query Batch Tests
// 3 Commands: ExportCustomers, RedeemPoints, SaveCrmSettings
// 11 Queries: GetActiveCampaigns, GetBitrix24Pipeline,
//   GetContactsPaged, GetCrmSettings, GetCustomerPoints,
//   GetDeals, GetLeads, GetLeadScore, GetPipelineKanban,
//   GetPlatformMessages, GetSuppliersCrm
// ═══════════════════════════════════════════════════════════════

#region ExportCustomersHandler

[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
public class ExportCustomersHandlerTests
{
    private readonly ExportCustomersHandler _sut = new();

    [Fact]
    public async Task Handle_ValidCommand_ReturnsResultWithFileName()
    {
        var cmd = new ExportCustomersCommand(Guid.NewGuid(), "csv");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.FileName.Should().Contain(".csv");
        result.ExportedCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NullCommand_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region RedeemPointsHandler

[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
public class RedeemPointsHandlerTests
{
    private readonly Mock<ILoyaltyProgramRepository> _programRepoMock = new();
    private readonly Mock<ILoyaltyTransactionRepository> _transactionRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly RedeemPointsHandler _sut;

    public RedeemPointsHandlerTests()
    {
        _sut = new RedeemPointsHandler(
            _programRepoMock.Object,
            _transactionRepoMock.Object,
            _uowMock.Object);
    }

    [Fact]
    public async Task Handle_NoProgramFound_ThrowsInvalidOperationException()
    {
        _programRepoMock.Setup(r => r.GetActiveByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LoyaltyProgram?)null);

        var cmd = new RedeemPointsCommand(Guid.NewGuid(), Guid.NewGuid(), 100);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NullCommand_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region SaveCrmSettingsHandler

[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
public class SaveCrmSettingsHandlerTests
{
    private readonly Mock<ICompanySettingsRepository> _settingsRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<SaveCrmSettingsHandler>> _loggerMock = new();
    private readonly SaveCrmSettingsHandler _sut;

    public SaveCrmSettingsHandlerTests()
    {
        _sut = new SaveCrmSettingsHandler(
            _settingsRepoMock.Object,
            _uowMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_NoExistingSettings_CreatesNewAndReturnsSuccess()
    {
        _settingsRepoMock.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanySettings?)null);

        var cmd = new SaveCrmSettingsCommand(Guid.NewGuid(), true, null, 50, false);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _settingsRepoMock.Verify(r => r.AddAsync(It.IsAny<CompanySettings>(), It.IsAny<CancellationToken>()), Times.Once);
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

#region GetActiveCampaignsHandler

[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
public class GetActiveCampaignsHandlerTests
{
    private readonly Mock<ICampaignRepository> _repoMock = new();
    private readonly GetActiveCampaignsHandler _sut;

    public GetActiveCampaignsHandlerTests()
    {
        _sut = new GetActiveCampaignsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_EmptyCampaigns_ReturnsEmptyResult()
    {
        _repoMock.Setup(r => r.GetActiveByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Campaign>().AsReadOnly());

        var query = new GetActiveCampaignsQuery(Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        _repoMock.Verify(r => r.GetActiveByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullQuery_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region GetBitrix24PipelineHandler

[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
public class GetBitrix24PipelineHandlerTests
{
    private readonly Mock<IPipelineRepository> _pipelineRepoMock = new();
    private readonly Mock<ILogger<GetBitrix24PipelineHandler>> _loggerMock = new();
    private readonly GetBitrix24PipelineHandler _sut;

    public GetBitrix24PipelineHandlerTests()
    {
        _sut = new GetBitrix24PipelineHandler(_pipelineRepoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_EmptyPipelines_ReturnsEmptyStages()
    {
        _pipelineRepoMock.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Pipeline>().AsReadOnly());

        var query = new GetBitrix24PipelineQuery(Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalDeals.Should().Be(0);
        result.Stages.Should().BeEmpty();
    }
}

#endregion

#region GetContactsPagedHandler

[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
public class GetContactsPagedHandlerTests
{
    private readonly Mock<ICrmContactRepository> _contactRepoMock = new();
    private readonly GetContactsPagedHandler _sut;

    public GetContactsPagedHandlerTests()
    {
        _sut = new GetContactsPagedHandler(_contactRepoMock.Object);
    }

    [Fact]
    public async Task Handle_EmptyContacts_ReturnsEmptyResult()
    {
        _contactRepoMock.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CrmContact>().AsReadOnly());

        var query = new GetContactsPagedQuery(Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Contacts.Should().BeEmpty();
    }
}

#endregion

#region GetCrmSettingsHandler

[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
public class GetCrmSettingsHandlerTests
{
    private readonly Mock<ICompanySettingsRepository> _settingsRepoMock = new();
    private readonly Mock<ILogger<GetCrmSettingsHandler>> _loggerMock = new();
    private readonly GetCrmSettingsHandler _sut;

    public GetCrmSettingsHandlerTests()
    {
        _sut = new GetCrmSettingsHandler(_settingsRepoMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_NoSettingsFound_ReturnsDefaults()
    {
        _settingsRepoMock.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanySettings?)null);

        var query = new GetCrmSettingsQuery(Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.AutoAssignLeads.Should().BeFalse();
        result.LeadScoreThreshold.Should().Be(50);
    }

    [Fact]
    public async Task Handle_NullQuery_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region GetCustomerPointsHandler

[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
public class GetCustomerPointsHandlerTests
{
    private readonly Mock<ILoyaltyTransactionRepository> _transRepoMock = new();
    private readonly GetCustomerPointsHandler _sut;

    public GetCustomerPointsHandlerTests()
    {
        _sut = new GetCustomerPointsHandler(_transRepoMock.Object);
    }

    [Fact]
    public async Task Handle_NoTransactions_ReturnsZeroBalance()
    {
        _transRepoMock.Setup(r => r.GetPointsSumByTypeAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<LoyaltyTransactionType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _transRepoMock.Setup(r => r.GetByCustomerPagedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LoyaltyTransaction>().AsReadOnly());

        var query = new GetCustomerPointsQuery(Guid.NewGuid(), Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.AvailableBalance.Should().Be(0);
        result.TransactionHistory.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullQuery_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region GetDealsHandler

[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
public class GetDealsHandlerTests
{
    private readonly Mock<ICrmDealRepository> _repoMock = new();
    private readonly GetDealsHandler _sut;

    public GetDealsHandlerTests()
    {
        _sut = new GetDealsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_EmptyDeals_ReturnsEmptyResult()
    {
        _repoMock.Setup(r => r.GetByTenantPagedAsync(
                It.IsAny<Guid>(), It.IsAny<DealStatus?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Deal>().AsReadOnly());

        var query = new GetDealsQuery(Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullQuery_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region GetLeadsHandler

[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
public class GetLeadsHandlerTests
{
    private readonly Mock<ICrmLeadRepository> _repoMock = new();
    private readonly GetLeadsHandler _sut;

    public GetLeadsHandlerTests()
    {
        _sut = new GetLeadsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_EmptyLeads_ReturnsEmptyResult()
    {
        _repoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<Guid>(), It.IsAny<LeadStatus?>(), It.IsAny<Guid?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Lead>().AsReadOnly(), 0));

        var query = new GetLeadsQuery(Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullQuery_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region GetLeadScoreHandler

[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
public class GetLeadScoreHandlerTests
{
    private readonly Mock<ILeadRepository> _leadRepoMock = new();
    private readonly GetLeadScoreHandler _sut;

    public GetLeadScoreHandlerTests()
    {
        _sut = new GetLeadScoreHandler(_leadRepoMock.Object);
    }

    [Fact]
    public async Task Handle_LeadNotFound_ReturnsZeroScore()
    {
        _leadRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Lead?)null);

        var query = new GetLeadScoreQuery(Guid.NewGuid(), Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Score.Should().Be(0);
        result.ScoreLabel.Should().Be("Not Found");
    }
}

#endregion

#region GetPipelineKanbanHandler

[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
public class GetPipelineKanbanHandlerTests
{
    private readonly Mock<ICrmDealRepository> _dealRepoMock = new();
    private readonly Mock<IPipelineRepository> _pipelineRepoMock = new();
    private readonly GetPipelineKanbanHandler _sut;

    public GetPipelineKanbanHandlerTests()
    {
        _sut = new GetPipelineKanbanHandler(_dealRepoMock.Object, _pipelineRepoMock.Object);
    }

    [Fact]
    public async Task Handle_PipelineNotFound_ThrowsInvalidOperationException()
    {
        _pipelineRepoMock.Setup(r => r.GetByIdWithStagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pipeline?)null);

        var query = new GetPipelineKanbanQuery(Guid.NewGuid(), Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.Handle(query, CancellationToken.None));
    }
}

#endregion

#region GetPlatformMessagesHandler

[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
public class GetPlatformMessagesHandlerTests
{
    private readonly Mock<IPlatformMessageRepository> _repoMock = new();
    private readonly GetPlatformMessagesHandler _sut;

    public GetPlatformMessagesHandlerTests()
    {
        _sut = new GetPlatformMessagesHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_EmptyMessages_ReturnsEmptyResult()
    {
        _repoMock.Setup(r => r.GetPagedAsync(
                It.IsAny<Guid>(), It.IsAny<PlatformType?>(), It.IsAny<MessageStatus?>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<PlatformMessage>().AsReadOnly(), 0));

        var query = new GetPlatformMessagesQuery(Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullQuery_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region GetSuppliersCrmHandler

[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
public class GetSuppliersCrmHandlerTests2
{
    private readonly Mock<ICrmDashboardQueryService> _queryServiceMock = new();
    private readonly GetSuppliersCrmHandler _sut;

    public GetSuppliersCrmHandlerTests2()
    {
        _sut = new GetSuppliersCrmHandler(_queryServiceMock.Object);
    }

    [Fact]
    public async Task Handle_EmptySuppliers_ReturnsEmptyResult()
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
        _queryServiceMock.Verify(s => s.GetSuppliersPagedAsync(
            It.IsAny<Guid>(), It.IsAny<bool?>(), It.IsAny<bool?>(),
            It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullQuery_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion
