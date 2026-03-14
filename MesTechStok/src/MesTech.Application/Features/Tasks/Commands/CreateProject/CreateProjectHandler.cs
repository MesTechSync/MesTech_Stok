using MediatR;
using MesTech.Domain.Entities.Tasks;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Tasks.Commands.CreateProject;

public class CreateProjectHandler : IRequestHandler<CreateProjectCommand, Guid>
{
    private readonly IProjectRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateProjectHandler(IProjectRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateProjectCommand req, CancellationToken ct)
    {
        var project = Project.Create(req.TenantId, req.Name, req.OwnerUserId,
            req.Description, req.StartDate, req.DueDate, req.Color);
        await _repository.AddAsync(project, ct);
        await _uow.SaveChangesAsync(ct);
        return project.Id;
    }
}
