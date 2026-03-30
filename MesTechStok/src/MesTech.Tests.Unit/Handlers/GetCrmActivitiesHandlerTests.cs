using FluentAssertions;
using MesTech.Application.Features.Crm.Queries.GetCrmActivities;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetCrmActivitiesHandlerTests
{
    private readonly Mock<IActivityRepository> _activityRepoMock = new();
    private readonly Mock<ICrmContactRepository> _contactRepoMock = new();
    private readonly Mock<ILogger<GetCrmActivitiesHandler>> _loggerMock = new();
    private readonly GetCrmActivitiesHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid ContactId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public GetCrmActivitiesHandlerTests()
    {
        _sut = new GetCrmActivitiesHandler(
            _activityRepoMock.Object,
            _contactRepoMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithoutContactId_FetchesByTenant()
    {
        // Arrange
        var activities = new List<Activity>
        {
            Activity.Create(TenantId, ActivityType.Call, "Follow-up call", UserId,
                occurredAt: DateTime.UtcNow.AddHours(-1), crmContactId: ContactId),
            Activity.Create(TenantId, ActivityType.Email, "Intro email", UserId,
                occurredAt: DateTime.UtcNow.AddHours(-2))
        };
        var contacts = new List<CrmContact>
        {
            CrmContact.Create(TenantId, "John Doe", ContactType.Individual)
        };
        // Patch the contact Id to match
        var contact = contacts[0];

        _activityRepoMock.Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activities);
        _contactRepoMock.Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contacts);

        var query = new GetCrmActivitiesQuery(TenantId, ContactId: null, Page: 1, PageSize: 50);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.Activities.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(50);
        _activityRepoMock.Verify(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()), Times.Once);
        _activityRepoMock.Verify(r => r.GetByContactAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithContactId_FetchesByContact()
    {
        // Arrange
        var activities = new List<Activity>
        {
            Activity.Create(TenantId, ActivityType.Meeting, "Quarterly review", UserId, crmContactId: ContactId)
        };
        var contacts = new List<CrmContact>();

        _activityRepoMock.Setup(r => r.GetByContactAsync(ContactId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activities);
        _contactRepoMock.Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contacts);

        var query = new GetCrmActivitiesQuery(TenantId, ContactId: ContactId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        _activityRepoMock.Verify(r => r.GetByContactAsync(ContactId, It.IsAny<CancellationToken>()), Times.Once);
        _activityRepoMock.Verify(r => r.GetByTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyActivities_ReturnsEmptyResult()
    {
        // Arrange
        _activityRepoMock.Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Activity>());
        _contactRepoMock.Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CrmContact>());

        var query = new GetCrmActivitiesQuery(TenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Activities.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Pagination_ReturnsCorrectPage()
    {
        // Arrange — create 5 activities, request page 2 with size 2
        var activities = Enumerable.Range(1, 5).Select(i =>
            Activity.Create(TenantId, ActivityType.Note, $"Note {i}", UserId,
                occurredAt: DateTime.UtcNow.AddMinutes(-i))
        ).ToList();

        _activityRepoMock.Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activities);
        _contactRepoMock.Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CrmContact>());

        var query = new GetCrmActivitiesQuery(TenantId, Page: 2, PageSize: 2);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(5);
        result.Activities.Should().HaveCount(2);
        result.Page.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ActivitiesOrderedByOccurredAtDescending()
    {
        // Arrange
        var older = Activity.Create(TenantId, ActivityType.Call, "Old call", UserId,
            occurredAt: DateTime.UtcNow.AddDays(-2));
        var newer = Activity.Create(TenantId, ActivityType.Email, "New email", UserId,
            occurredAt: DateTime.UtcNow);

        _activityRepoMock.Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Activity> { older, newer });
        _contactRepoMock.Setup(r => r.GetByTenantAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CrmContact>());

        var query = new GetCrmActivitiesQuery(TenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert — newer should come first
        result.Activities[0].Subject.Should().Be("New email");
        result.Activities[1].Subject.Should().Be("Old call");
    }
}
