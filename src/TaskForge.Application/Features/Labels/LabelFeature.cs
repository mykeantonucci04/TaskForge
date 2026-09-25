using FluentValidation;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Common.Messaging;
using TaskForge.Application.Features.Tasks;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Interfaces;
using TaskForge.Shared.Results;

namespace TaskForge.Application.Features.Labels;

public record CreateLabelCommand(Guid WorkspaceId, string Name, string ColorHex) : ICommand<LabelDto>;

public record GetLabelsByWorkspaceQuery(Guid WorkspaceId) : IQuery<IReadOnlyList<LabelDto>>;

public record AssignLabelToTaskCommand(Guid TaskId, Guid LabelId) : ICommand<bool>;

public record RemoveLabelFromTaskCommand(Guid TaskId, Guid LabelId) : ICommand<bool>;

public class CreateLabelCommandValidator : AbstractValidator<CreateLabelCommand>
{
    public CreateLabelCommandValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ColorHex)
            .NotEmpty()
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .WithMessage("Color must be a valid 6-character hex color (e.g. #FF5733).");
    }
}

public class CreateLabelCommandHandler(
    ILabelRepository labelRepository,
    ICurrentTenantService tenantService,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateLabelCommand, LabelDto>
{
    public async Task<Result<LabelDto>> Handle(CreateLabelCommand request, CancellationToken cancellationToken)
    {
        if (!tenantService.TenantId.HasValue)
        {
            return Result.Failure<LabelDto>(
                Error.Validation("Tenant.Required", "Tenant context is required to create a label."));
        }

        var trimmedName = request.Name.Trim();
        var exists = await labelRepository.ExistsByNameAsync(request.WorkspaceId, trimmedName, cancellationToken);
        if (exists)
        {
            return Result.Failure<LabelDto>(
                Error.Conflict("Label.Exists", $"A label named '{trimmedName}' already exists in this workspace."));
        }

        var label = new Label
        {
            Id = Guid.NewGuid(),
            TenantId = tenantService.TenantId.Value,
            WorkspaceId = request.WorkspaceId,
            Name = trimmedName,
            ColorHex = request.ColorHex.ToUpperInvariant()
        };

        await labelRepository.AddAsync(label, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new LabelDto(label.Id, label.WorkspaceId, label.Name, label.ColorHex));
    }
}

public class GetLabelsByWorkspaceQueryHandler(
    ILabelRepository labelRepository) : IQueryHandler<GetLabelsByWorkspaceQuery, IReadOnlyList<LabelDto>>
{
    public async Task<Result<IReadOnlyList<LabelDto>>> Handle(GetLabelsByWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var labels = await labelRepository.GetLabelsByWorkspaceAsync(request.WorkspaceId, cancellationToken);
        var dtos = labels.Select(l => new LabelDto(l.Id, l.WorkspaceId, l.Name, l.ColorHex)).ToList();
        return Result.Success<IReadOnlyList<LabelDto>>(dtos);
    }
}

public class AssignLabelToTaskCommandHandler(
    ITaskRepository taskRepository,
    ILabelRepository labelRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<AssignLabelToTaskCommand, bool>
{
    public async Task<Result<bool>> Handle(AssignLabelToTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<bool>(
                Error.NotFound("Task.NotFound", $"Task with ID '{request.TaskId}' was not found."));
        }

        var label = await labelRepository.GetByIdAsync(request.LabelId, cancellationToken);
        if (label is null)
        {
            return Result.Failure<bool>(
                Error.NotFound("Label.NotFound", $"Label with ID '{request.LabelId}' was not found."));
        }

        if (!task.Labels.Any(l => l.Id == request.LabelId))
        {
            task.Labels.Add(label);
            task.LastModifiedAtUtc = DateTime.UtcNow;
            taskRepository.Update(task);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(true);
    }
}

public class RemoveLabelFromTaskCommandHandler(
    ITaskRepository taskRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<RemoveLabelFromTaskCommand, bool>
{
    public async Task<Result<bool>> Handle(RemoveLabelFromTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<bool>(
                Error.NotFound("Task.NotFound", $"Task with ID '{request.TaskId}' was not found."));
        }

        var labelToRemove = task.Labels.FirstOrDefault(l => l.Id == request.LabelId);
        if (labelToRemove is not null)
        {
            task.Labels.Remove(labelToRemove);
            task.LastModifiedAtUtc = DateTime.UtcNow;
            taskRepository.Update(task);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(true);
    }
}
