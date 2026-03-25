using MediatR;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Tasks.Commands.CreateProject;

public sealed class CreateProjectHandler : IRequestHandler<CreateProjectCommand, Guid>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateProjectHandler(IProjectRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var project = Project.Create(request.TenantId, request.Name, request.OwnerUserId,
            request.Description, request.StartDate, request.DueDate, request.Color);
        await _repository.AddAsync(project, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return project.Id;
    }
}
