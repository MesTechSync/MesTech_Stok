using MediatR;

namespace MesTech.Application.Features.System.Users;

public record GetUsersQuery(Guid? TenantId = null) : IRequest<IReadOnlyList<UserListItemDto>>;

public record UserListItemDto(
    Guid Id,
    string Username,
    string FullName,
    string? Email,
    string Role,
    bool IsActive,
    DateTime? LastLoginDate);
