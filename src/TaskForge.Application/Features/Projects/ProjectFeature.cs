using FluentValidation;
using Riok.Mapperly.Abstractions;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Common.Messaging;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Interfaces;
using TaskForge.Shared.Results;

namespace TaskForge.Application.Features.Projects;

public record ProjectDto(
    Guid Id,
    Guid TenantId,
    Guid WorkspaceId,
    string Name,
    string Key,
    string? Description,
    DateTime CreatedAtUtc);

public record BoardSummaryDto(
    Guid Id,
    string Name,
    DateTime CreatedAtUtc);

public record ProjectDetailDto(
    Guid Id,
    Guid TenantId,
    Guid WorkspaceId,
    string Name,
    string Key,
    string? Description,
    DateTime CreatedAtUtc,
    IReadOnlyList<BoardSummaryDto> Boards);

public record CreateProjectCommand(
    Guid WorkspaceId,
    string Name,
    string Key,
    string? Description) : ICommand<ProjectDto>;

public record UpdateProjectCommand(
    Guid ProjectId,
    string Name,
    string? Description) : ICommand<ProjectDto>;

public record DeleteProjectCommand(Guid ProjectId) : ICommand<bool>;

public record GetProjectsByWorkspaceQuery(Guid WorkspaceId) : IQuery<IReadOnlyList<ProjectDto>>;

public record GetProjectByIdQuery(Guid Id) : IQuery<ProjectDetailDto>;

[Mapper]
public partial class ProjectMapper
{
    public partial ProjectDto ProjectToProjectDto(Project project);
    public partial IReadOnlyList<ProjectDto> ProjectsToProjectDtos(IReadOnlyList<Project> projects);

    public ProjectDetailDto ProjectToProjectDetailDto(Project project)
    {
        var boards = project.Boards?.Select(b => new BoardSummaryDto(b.Id, b.Name, b.CreatedAtUtc)).ToList() 
                     ?? new List<BoardSummaryDto>();

        return new ProjectDetailDto(
            project.Id,
            project.TenantId,
            project.WorkspaceId,
            project.Name,
            project.Key,
            project.Description,
            project.CreatedAtUtc,
            boards);
    }
}

public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Key)
            .NotEmpty()
            .Length(2, 10)
            .Matches(@"^[A-Za-z0-9]{2,10}$")
            .WithMessage("Project key must be 2-10 alphanumeric characters.");
    }
}

public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateProjectCommandHandler(
    IProjectRepository projectRepository,
    ICurrentTenantService tenantService,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateProjectCommand, ProjectDto>
{
    public async Task<Result<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        if (!tenantService.TenantId.HasValue)
        {
            return Result.Failure<ProjectDto>(
                Error.Validation("Tenant.Required", "Tenant context is required to create a project."));
        }

        var normalizedKey = request.Key.Trim().ToUpperInvariant();
        var keyExists = await projectRepository.ExistsByKeyAsync(request.WorkspaceId, normalizedKey, cancellationToken);
        if (keyExists)
        {
            return Result.Failure<ProjectDto>(
                Error.Conflict("Project.KeyExists", $"A project with key '{normalizedKey}' already exists in this workspace."));
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            TenantId = tenantService.TenantId.Value,
            WorkspaceId = request.WorkspaceId,
            Name = request.Name.Trim(),
            Key = normalizedKey,
            Description = request.Description?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        await projectRepository.AddAsync(project, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var mapper = new ProjectMapper();
        return Result.Success(mapper.ProjectToProjectDto(project));
    }
}

public class GetProjectsByWorkspaceQueryHandler(
    IProjectRepository projectRepository) : IQueryHandler<GetProjectsByWorkspaceQuery, IReadOnlyList<ProjectDto>>
{
    public async Task<Result<IReadOnlyList<ProjectDto>>> Handle(GetProjectsByWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var projects = await projectRepository.GetProjectsByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        var mapper = new ProjectMapper();
        return Result.Success(mapper.ProjectsToProjectDtos(projects));
    }
}

public class GetProjectByIdQueryHandler(
    IProjectRepository projectRepository) : IQueryHandler<GetProjectByIdQuery, ProjectDetailDto>
{
    public async Task<Result<ProjectDetailDto>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdWithBoardsAsync(request.Id, cancellationToken);
        if (project is null)
        {
            return Result.Failure<ProjectDetailDto>(
                Error.NotFound("Project.NotFound", $"Project with ID '{request.Id}' was not found."));
        }

        var mapper = new ProjectMapper();
        return Result.Success(mapper.ProjectToProjectDetailDto(project));
    }
}

public class UpdateProjectCommandHandler(
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateProjectCommand, ProjectDto>
{
    public async Task<Result<ProjectDto>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return Result.Failure<ProjectDto>(
                Error.NotFound("Project.NotFound", $"Project with ID '{request.ProjectId}' was not found."));
        }

        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();
        project.LastModifiedAtUtc = DateTime.UtcNow;

        projectRepository.Update(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var mapper = new ProjectMapper();
        return Result.Success(mapper.ProjectToProjectDto(project));
    }
}

public class DeleteProjectCommandHandler(
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteProjectCommand, bool>
{
    public async Task<Result<bool>> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return Result.Failure<bool>(
                Error.NotFound("Project.NotFound", $"Project with ID '{request.ProjectId}' was not found."));
        }

        projectRepository.Remove(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
