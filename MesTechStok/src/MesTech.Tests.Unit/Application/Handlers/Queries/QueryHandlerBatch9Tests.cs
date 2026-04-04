using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetWithholdingRates;
using MesTech.Application.Features.Finance.Queries.GetCashRegisters;
using MesTech.Application.Features.Hr.Queries.GetDepartments;
using MesTech.Application.Features.Hr.Queries.GetLeaveRequests;
using MesTech.Application.Features.Logging.Queries.GetLogCount;
using MesTech.Application.Features.Logging.Queries.GetLogs;
using MesTech.Application.Features.Onboarding.Queries.GetOnboardingProgress;
using MesTech.Application.Features.Tasks.Queries.GetProjectTasks;
using MesTech.Application.Features.Tenant.Queries.GetTenants;
using MesTech.Application.Queries.GetWarehouseById;
using MesTech.Application.Queries.GetWarehouses;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Entities.Hr;
using MesTech.Domain.Entities.Onboarding;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

/// <summary>
/// DEV5 Batch 9: Toplu query handler testleri — 10 handler.
/// </summary>

#region GetCashRegisters
[Trait("Category", "Unit")]
public class GetCashRegistersHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnRegistersFromRepo()
    {
        var repo = new Mock<ICashRegisterRepository>();
        repo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CashRegister>());
        var handler = new GetCashRegistersHandler(repo.Object);
        var result = await handler.Handle(new GetCashRegistersQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeEmpty();
    }
}
#endregion

#region GetDepartments
[Trait("Category", "Unit")]
public class GetDepartmentsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnDepartments()
    {
        var repo = new Mock<IDepartmentRepository>();
        repo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department>());
        var handler = new GetDepartmentsHandler(repo.Object);
        var result = await handler.Handle(new GetDepartmentsQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeEmpty();
    }
}
#endregion

#region GetLogCount
[Trait("Category", "Unit")]
public class GetLogCountHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnCount()
    {
        var repo = new Mock<ILogEntryRepository>();
        repo.Setup(r => r.GetCountAsync(It.IsAny<Guid>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42L);
        var handler = new GetLogCountHandler(repo.Object);
        var result = await handler.Handle(new GetLogCountQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().Be(42);
    }
}
#endregion

#region GetLogs
[Trait("Category", "Unit")]
public class GetLogsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnPagedLogs()
    {
        var repo = new Mock<ILogEntryRepository>();
        repo.Setup(r => r.GetPagedAsync(
            It.IsAny<Guid>(), 1, 50, null, null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LogEntry>());
        var handler = new GetLogsHandler(repo.Object);
        var result = await handler.Handle(new GetLogsQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeEmpty();
    }
}
#endregion

#region GetProjectTasks
[Trait("Category", "Unit")]
public class GetProjectTasksHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnTasks()
    {
        var repo = new Mock<IWorkTaskRepository>();
        repo.Setup(r => r.GetByProjectAsync(It.IsAny<Guid>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkTask>());
        var handler = new GetProjectTasksHandler(repo.Object);
        var result = await handler.Handle(new GetProjectTasksQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeEmpty();
    }
}
#endregion

#region GetOnboardingProgress
[Trait("Category", "Unit")]
public class GetOnboardingProgressHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_ShouldReturnNull()
    {
        var repo = new Mock<IOnboardingProgressRepository>();
        repo.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingProgress?)null);
        var handler = new GetOnboardingProgressHandler(repo.Object);
        var result = await handler.Handle(new GetOnboardingProgressQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeNull();
    }
}
#endregion

#region GetWarehouseById
[Trait("Category", "Unit")]
public class GetWarehouseByIdHandlerTests
{
    [Fact]
    public async Task Handle_Found_ShouldReturnDto()
    {
        var wh = new Warehouse { Name = "Ana Depo", Code = "AD", TenantId = Guid.NewGuid() };
        var repo = new Mock<IWarehouseRepository>();
        repo.Setup(r => r.GetByIdAsync(wh.Id)).ReturnsAsync(wh);
        var handler = new GetWarehouseByIdHandler(repo.Object);
        var result = await handler.Handle(new GetWarehouseByIdQuery(wh.Id), CancellationToken.None);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Ana Depo");
    }

    [Fact]
    public async Task Handle_NotFound_ShouldReturnNull()
    {
        var repo = new Mock<IWarehouseRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Warehouse?)null);
        var handler = new GetWarehouseByIdHandler(repo.Object);
        var result = await handler.Handle(new GetWarehouseByIdQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeNull();
    }
}
#endregion

#region GetTenants
[Trait("Category", "Unit")]
public class GetTenantsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnPagedTenants()
    {
        var repo = new Mock<ITenantRepository>();
        repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.Tenant>());
        var handler = new GetTenantsHandler(repo.Object);
        var result = await handler.Handle(new GetTenantsQuery(), CancellationToken.None);
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
}
#endregion

#region GetLeaveRequests
[Trait("Category", "Unit")]
public class GetLeaveRequestsHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnLeaves()
    {
        var repo = new Mock<ILeaveRepository>();
        repo.Setup(r => r.GetByTenantAsync(It.IsAny<Guid>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Leave>());
        var handler = new GetLeaveRequestsHandler(repo.Object);
        var result = await handler.Handle(new GetLeaveRequestsQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeEmpty();
    }
}
#endregion

#region GetWithholdingRates
[Trait("Category", "Unit")]
public class GetWithholdingRatesHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnNonEmptyList()
    {
        var handler = new GetWithholdingRatesHandler();
        var result = await handler.Handle(new GetWithholdingRatesQuery(), CancellationToken.None);
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_EachRateShouldHaveCode()
    {
        var handler = new GetWithholdingRatesHandler();
        var result = await handler.Handle(new GetWithholdingRatesQuery(), CancellationToken.None);
        result.Should().AllSatisfy(r => r.Code.Should().NotBeNullOrEmpty());
    }
}
#endregion
