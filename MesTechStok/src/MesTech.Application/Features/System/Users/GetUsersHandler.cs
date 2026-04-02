using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.System.Users;

public sealed class GetUsersHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserListItemDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUsersHandler> _logger;

    public GetUsersHandler(IUserRepository userRepository, ILogger<GetUsersHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserListItemDto>> Handle(
        GetUsersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting users list, TenantId={TenantId}", request.TenantId);

        var users = await _userRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        if (request.TenantId.HasValue)
            users = users.Where(u => u.TenantId == request.TenantId.Value).ToList();

        return users.Select(u => new UserListItemDto(
            u.Id,
            u.Username,
            u.FullName ?? u.Username,
            u.Email,
            "User",
            u.IsActive,
            u.LastLoginDate
        )).ToList().AsReadOnly();
    }
}
