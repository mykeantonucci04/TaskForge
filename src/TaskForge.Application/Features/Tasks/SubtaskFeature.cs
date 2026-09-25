using FluentValidation;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Common.Messaging;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Interfaces;
using TaskForge.Shared.Results;

namespace TaskForge.Application.Features.Tasks;

public record AddSubtaskCommand(Guid TaskId, string Title) : ICommand<SubtaskDto>;

public record ToggleSubtaskCommand(Guid SubtaskId) : ICommand<SubtaskDto>;

public record DeleteSubtaskCommand(Guid SubtaskId) : ICommand<bool>;

public record GetSubtasksByTaskQuery(Guid TaskId) : IQuery<IReadOnlyList<SubtaskDto>>;

public class AddSubtaskCommandValidator : AbstractValidator<AddSubtaskCommand>
{
    public AddSubtaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public class AddSubtaskCommandHandler(
    ITaskRepository taskRepository,
    ISubtaskRepository subtaskRepository,
    ICurrentTenantService tenantService,
    IUnitOfWork unitOfWork) : ICommandHandler<AddSubtaskCommand, SubtaskDto>
{
    public async Task<Result<SubtaskDto>> Handle(AddSubtaskCommand request, CancellationToken cancellationToken)
    {
        if (!tenantService.TenantId.HasValue)
        {
            return Result.Failure<SubtaskDto>(
                Error.Validation("Tenant.Required", "Tenant context is required to create a subtask."));
        }

        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<SubtaskDto>(
                Error.NotFound("Task.NotFound", $"Task with ID '{request.TaskId}' was not found."));
        }

        var subtask = new Subtask
        {
            Id = Guid.NewGuid(),
            TenantId = tenantService.TenantId.Value,
            TaskItemId = request.TaskId,
            Title = request.Title.Trim(),
            IsCompleted = false
        };

        await subtaskRepository.AddAsync(subtask, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new SubtaskDto(subtask.Id, subtask.TaskItemId, subtask.Title, subtask.IsCompleted));
    }
}

public class ToggleSubtaskCommandHandler(
    ISubtaskRepository subtaskRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ToggleSubtaskCommand, SubtaskDto>
{
    public async Task<Result<SubtaskDto>> Handle(ToggleSubtaskCommand request, CancellationToken cancellationToken)
    {
        var subtask = await subtaskRepository.GetByIdAsync(request.SubtaskId, cancellationToken);
        if (subtask is null)
        {
            return Result.Failure<SubtaskDto>(
                Error.NotFound("Subtask.NotFound", $"Subtask with ID '{request.SubtaskId}' was not found."));
        }

        subtask.IsCompleted = !subtask.IsCompleted;

        subtaskRepository.Update(subtask);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new SubtaskDto(subtask.Id, subtask.TaskItemId, subtask.Title, subtask.IsCompleted));
    }
}

public class DeleteSubtaskCommandHandler(
    ISubtaskRepository subtaskRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteSubtaskCommand, bool>
{
    public async Task<Result<bool>> Handle(DeleteSubtaskCommand request, CancellationToken cancellationToken)
    {
        var subtask = await subtaskRepository.GetByIdAsync(request.SubtaskId, cancellationToken);
        if (subtask is null)
        {
            return Result.Failure<bool>(
                Error.NotFound("Subtask.NotFound", $"Subtask with ID '{request.SubtaskId}' was not found."));
        }

        subtaskRepository.Remove(subtask);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}

public class GetSubtasksByTaskQueryHandler(
    ISubtaskRepository subtaskRepository) : IQueryHandler<GetSubtasksByTaskQuery, IReadOnlyList<SubtaskDto>>
{
    public async Task<Result<IReadOnlyList<SubtaskDto>>> Handle(GetSubtasksByTaskQuery request, CancellationToken cancellationToken)
    {
        var subtasks = await subtaskRepository.GetSubtasksByTaskAsync(request.TaskId, cancellationToken);
        var dtos = subtasks.Select(s => new SubtaskDto(s.Id, s.TaskItemId, s.Title, s.IsCompleted)).ToList();
        return Result.Success<IReadOnlyList<SubtaskDto>>(dtos);
    }
}
