using FluentAssertions;
using MesTech.Application.Features.Hr.Queries.GetTimeEntries;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Hr;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetTimeEntriesHandlerTests
{
    private readonly Mock<ITimeEntryRepository> _repo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetTimeEntriesHandler CreateSut() => new(_repo.Object);

    [Fact]
    public void Constructor_NullRepo_Throws()
    {
        var act = () => new GetTimeEntriesHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyList()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        _repo.Setup(r => r.GetByTenantAsync(
                _tenantId, from, to, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeEntry>().AsReadOnly());

        var result = await CreateSut().Handle(
            new GetTimeEntriesQuery(_tenantId, from, to), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithEntries_MapsAllFields()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var entry = TimeEntry.Start(_tenantId, taskId, userId,
            "Code review", true, 150m);
        _repo.Setup(r => r.GetByTenantAsync(
                _tenantId, from, to, null, 1, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeEntry> { entry }.AsReadOnly());

        var result = await CreateSut().Handle(
            new GetTimeEntriesQuery(_tenantId, from, to), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].UserId.Should().Be(userId);
        result[0].WorkTaskId.Should().Be(taskId);
        result[0].Description.Should().Be("Code review");
        result[0].IsBillable.Should().BeTrue();
        result[0].HourlyRate.Should().Be(150m);
    }

    [Fact]
    public async Task Handle_WithUserFilter_PassesFilterToRepo()
    {
        var from = DateTime.UtcNow.AddDays(-30);
        var to = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        _repo.Setup(r => r.GetByTenantAsync(
                _tenantId, from, to, userId, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TimeEntry>().AsReadOnly());

        await CreateSut().Handle(
            new GetTimeEntriesQuery(_tenantId, from, to, UserId: userId, Page: 2, PageSize: 10),
            CancellationToken.None);

        _repo.Verify(r => r.GetByTenantAsync(
            _tenantId, from, to, userId, 2, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
