using FluentValidation;
using Riok.Mapperly.Abstractions;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Common.Messaging;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Interfaces;

namespace TaskForge.Application.Features.Workspaces;

public record WorkspaceDto(Guid Id, Guid TenantId, string Name, string Slug, string? Description, DateTime CreatedAtUtc);

public record CreateWorkspaceCommand(string Name, string Slug, string? Description) : ICommand<WorkspaceDto>;

public record GetWorkspacesQuery() : IQuery<IReadOnlyList<WorkspaceDto>>;

[Mapper]
public partial class WorkspaceMapper
{
    public partial WorkspaceDto WorkspaceToWorkspaceDto(Workspace workspace);
    public partial IReadOnlyList<WorkspaceDto> WorkspacesToWorkspaceDtos(IReadOnlyList<Workspace> workspaces);
}

public class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(50).Matches("^[a-z0-9-]+$");
    }
}

public class CreateWorkspaceCommandHandler(
    IWorkspaceRepository workspaceRepository,
    ICurrentTenantService tenantService,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateWorkspaceCommand, WorkspaceDto>
{
    public async Task<Shared.Results.Result<WorkspaceDto>> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        if (!tenantService.TenantId.HasValue)
        {
            return Shared.Results.Result.Failure<WorkspaceDto>(
                Shared.Results.Error.Validation("Tenant.Required", "Tenant context is required to create a workspace."));
        }

        var existing = await workspaceRepository.GetBySlugAsync(request.Slug, cancellationToken);
        if (existing is not null)
        {
            return Shared.Results.Result.Failure<WorkspaceDto>(
                Shared.Results.Error.Conflict("Workspace.Exists", $"Workspace with slug '{request.Slug}' already exists."));
        }

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            TenantId = tenantService.TenantId.Value,
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            CreatedAtUtc = DateTime.UtcNow
        };

        await workspaceRepository.AddAsync(workspace, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var mapper = new WorkspaceMapper();
        return mapper.WorkspaceToWorkspaceDto(workspace);
    }
}

public class GetWorkspacesQueryHandler(
    IWorkspaceRepository workspaceRepository) : IQueryHandler<GetWorkspacesQuery, IReadOnlyList<WorkspaceDto>>
{
    public async Task<Shared.Results.Result<IReadOnlyList<WorkspaceDto>>> Handle(GetWorkspacesQuery request, CancellationToken cancellationToken)
    {
        var workspaces = await workspaceRepository.GetAllAsync(cancellationToken);
        var mapper = new WorkspaceMapper();
        return Shared.Results.Result.Success(mapper.WorkspacesToWorkspaceDtos(workspaces));
    }
}
