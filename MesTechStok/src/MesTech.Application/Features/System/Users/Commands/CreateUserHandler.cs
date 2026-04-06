using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.System.Users.Commands;

public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CreateUserHandler> _logger;

    public CreateUserHandler(
        IUserRepository userRepo,
        IUnitOfWork uow,
        ILogger<CreateUserHandler> logger)
    {
        _userRepo = userRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<CreateUserResult> Handle(
        CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Duplicate username check
        var existing = await _userRepo.GetByUsernameAsync(request.Username, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            _logger.LogWarning("CreateUser failed: username '{Username}' already exists", request.Username);
            return CreateUserResult.Fail($"'{request.Username}' kullanıcı adı zaten mevcut.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _userRepo.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("User created: {UserId} ({Username}) in tenant {TenantId}",
            user.Id, user.Username, user.TenantId);

        return CreateUserResult.Ok(user.Id);
    }
}
