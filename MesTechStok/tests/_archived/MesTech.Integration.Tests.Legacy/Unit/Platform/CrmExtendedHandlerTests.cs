using FluentAssertions;
using MesTech.Application.Features.Crm.Commands.UpdateDealStage;
using MesTech.Application.Features.Crm.Commands.ReplyToMessage;
using MesTech.Application.Features.Crm.Queries.GetActiveCampaigns;
using MesTech.Application.Features.Crm.Queries.GetDeals;
using MesTech.Application.Features.Crm.Queries.GetPipelineKanban;
using MesTech.Application.Features.Crm.Queries.GetPlatformMessages;
using MesTech.Application.Features.Crm.Queries.GetCustomersCrm;
using MesTech.Application.Features.Crm.Queries.GetSuppliersCrm;
using MesTech.Application.Features.Crm.Queries.GetLeadScore;
using MesTech.Application.Features.Crm.Queries.GetContactsPaged;
using MesTech.Application.Features.Crm.Queries.GetBitrix24Pipeline;
using MesTech.Application.Features.Crm.Queries.GetBitrix24Deals;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

[Trait("Category", "Unit")]
[Trait("Layer", "CRM")]
[Trait("Group", "Handler-Extended")]
public class CrmExtendedHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    public CrmExtendedHandlerTests() => _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

    [Fact] public async Task UpdateDealStage_NullRequest_Throws() { var r = new Mock<IDealRepository>(); var h = new UpdateDealStageHandler(r.Object, _uow.Object, Mock.Of<ILogger<UpdateDealStageHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task ReplyToMessage_NullRequest_Throws() { var r = new Mock<IPlatformMessageRepository>(); var h = new ReplyToMessageHandler(r.Object, _uow.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetActiveCampaigns_NullRequest_Throws() { var r = new Mock<ICampaignRepository>(); var h = new GetActiveCampaignsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetDeals_NullRequest_Throws() { var r = new Mock<ICrmDealRepository>(); var h = new GetDealsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetPipelineKanban_NullRequest_Throws() { var d = new Mock<ICrmDealRepository>(); var p = new Mock<IPipelineRepository>(); var h = new GetPipelineKanbanHandler(d.Object, p.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetPlatformMessages_NullRequest_Throws() { var r = new Mock<IPlatformMessageRepository>(); var h = new GetPlatformMessagesHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetCustomersCrm_NullRequest_Throws() { var s = new Mock<ICrmDashboardQueryService>(); var h = new GetCustomersCrmHandler(s.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetSuppliersCrm_NullRequest_Throws() { var s = new Mock<ICrmDashboardQueryService>(); var h = new GetSuppliersCrmHandler(s.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetLeadScore_NullRequest_Throws() { var r = new Mock<ILeadRepository>(); var h = new GetLeadScoreHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetContactsPaged_NullRequest_Throws() { var r = new Mock<ICrmContactRepository>(); var h = new GetContactsPagedHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetBitrix24Pipeline_NullRequest_Throws() { var r = new Mock<IPipelineRepository>(); var h = new GetBitrix24PipelineHandler(r.Object, Mock.Of<ILogger<GetBitrix24PipelineHandler>>()); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
    [Fact] public async Task GetBitrix24Deals_NullRequest_Throws() { var r = new Mock<IDealRepository>(); var h = new GetBitrix24DealsHandler(r.Object); await Assert.ThrowsAnyAsync<Exception>(() => h.Handle(null!, CancellationToken.None)); }
}
