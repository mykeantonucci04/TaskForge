using FluentValidation;
using Riok.Mapperly.Abstractions;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Common.Messaging;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Enums;
using TaskForge.Domain.Interfaces;
using TaskForge.Shared.Results;

namespace TaskForge.Application.Features.Tasks;

public record TaskDto(
    Guid Id,
    Guid ColumnId,
    Guid? AssigneeId,
    string Title,
    string? Description,
    TaskPriority Priority,
    TaskItemStatus Status,
    double Position,
    DateTime? DueDateUtc,
    DateTime CreatedAtUtc);

public record SubtaskDto(
    Guid Id,
    Guid TaskItemId,
    string Title,
    bool IsCompleted);

public record CommentDto(
    Guid Id,
    Guid TaskItemId,
    Guid AuthorId,
    string Content,
    DateTime CreatedAtUtc);

public record LabelDto(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string ColorHex);

public record TaskDetailDto(
    Guid Id,
    Guid ColumnId,
    Guid? AssigneeId,
    string Title,
    string? Description,
    TaskPriority Priority,
    TaskItemStatus Status,
    double Position,
    DateTime? DueDateUtc,
    DateTime CreatedAtUtc,
    IReadOnlyList<SubtaskDto> Subtasks,
    IReadOnlyList<CommentDto> Comments,
    IReadOnlyList<LabelDto> Labels);

public record CreateTaskCommand(
    Guid ColumnId,
    string Title,
    string? Description,
    TaskPriority Priority,
    Guid? AssigneeId,
    DateTime? DueDateUtc) : ICommand<TaskDto>;

public record UpdateTaskCommand(
    Guid TaskId,
    string Title,
    string? Description,
    TaskPriority Priority,
    Guid? AssigneeId,
    DateTime? DueDateUtc) : ICommand<TaskDto>;

public record DeleteTaskCommand(Guid TaskId) : ICommand<bool>;

public record MoveTaskCommand(
    Guid TaskId,
    Guid TargetColumnId,
    double NewPosition,
    TaskItemStatus? NewStatus = null) : ICommand<TaskDto>;

public record UpdateTaskStatusCommand(Guid TaskId, TaskItemStatus NewStatus) : ICommand<TaskDto>;

public record GetTaskByIdQuery(Guid TaskId) : IQuery<TaskDetailDto>;

public record GetTasksByColumnQuery(Guid ColumnId) : IQuery<IReadOnlyList<TaskDto>>;

public record SearchTasksQuery(
    Guid? ColumnId = null,
    Guid? AssigneeId = null,
    TaskPriority? Priority = null,
    TaskItemStatus? Status = null,
    Guid? LabelId = null) : IQuery<IReadOnlyList<TaskDto>>;

[Mapper]
public partial class TaskMapper
{
    public partial TaskDto TaskToTaskDto(TaskItem taskItem);
    public partial IReadOnlyList<TaskDto> TasksToTaskDtos(IReadOnlyList<TaskItem> taskItems);
    public partial SubtaskDto SubtaskToSubtaskDto(Subtask subtask);
    public partial CommentDto CommentToCommentDto(Comment comment);
    public partial LabelDto LabelToLabelDto(Label label);

    public TaskDetailDto TaskToTaskDetailDto(TaskItem task)
    {
        var subtasks = task.Subtasks?.Select(s => new SubtaskDto(s.Id, s.TaskItemId, s.Title, s.IsCompleted)).ToList()
                       ?? new List<SubtaskDto>();
        var comments = task.Comments?.OrderBy(c => c.CreatedAtUtc).Select(c => new CommentDto(c.Id, c.TaskItemId, c.AuthorId, c.Content, c.CreatedAtUtc)).ToList()
                       ?? new List<CommentDto>();
        var labels = task.Labels?.Select(l => new LabelDto(l.Id, l.WorkspaceId, l.Name, l.ColorHex)).ToList()
                     ?? new List<LabelDto>();

        return new TaskDetailDto(
            task.Id,
            task.ColumnId,
            task.AssigneeId,
            task.Title,
            task.Description,
            task.Priority,
            task.Status,
            task.Position,
            task.DueDateUtc,
            task.CreatedAtUtc,
            subtasks,
            comments,
            labels);
    }
}

public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.ColumnId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public class MoveTaskCommandValidator : AbstractValidator<MoveTaskCommand>
{
    public MoveTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.TargetColumnId).NotEmpty();
        RuleFor(x => x.NewPosition).GreaterThanOrEqualTo(0);
    }
}

public class CreateTaskCommandHandler(
    ITaskRepository taskRepository,
    ICurrentTenantService tenantService,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateTaskCommand, TaskDto>
{
    public async Task<Result<TaskDto>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        if (!tenantService.TenantId.HasValue)
        {
            return Result.Failure<TaskDto>(
                Error.Validation("Tenant.Required", "Tenant context is required to create a task."));
        }

        var taskItem = new TaskItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantService.TenantId.Value,
            ColumnId = request.ColumnId,
            AssigneeId = request.AssigneeId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = request.Priority,
            Status = TaskItemStatus.Todo,
            Position = 1000.0,
            DueDateUtc = request.DueDateUtc,
            CreatedAtUtc = DateTime.UtcNow
        };

        await taskRepository.AddAsync(taskItem, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var mapper = new TaskMapper();
        return Result.Success(mapper.TaskToTaskDto(taskItem));
    }
}

public class GetTaskByIdQueryHandler(
    ITaskRepository taskRepository) : IQueryHandler<GetTaskByIdQuery, TaskDetailDto>
{
    public async Task<Result<TaskDetailDto>> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetTaskWithDetailsAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<TaskDetailDto>(
                Error.NotFound("Task.NotFound", $"Task with ID '{request.TaskId}' was not found."));
        }

        var mapper = new TaskMapper();
        return Result.Success(mapper.TaskToTaskDetailDto(task));
    }
}

public class UpdateTaskCommandHandler(
    ITaskRepository taskRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateTaskCommand, TaskDto>
{
    public async Task<Result<TaskDto>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<TaskDto>(
                Error.NotFound("Task.NotFound", $"Task with ID '{request.TaskId}' was not found."));
        }

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        task.Priority = request.Priority;
        task.AssigneeId = request.AssigneeId;
        task.DueDateUtc = request.DueDateUtc;
        task.LastModifiedAtUtc = DateTime.UtcNow;

        taskRepository.Update(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var mapper = new TaskMapper();
        return Result.Success(mapper.TaskToTaskDto(task));
    }
}

public class DeleteTaskCommandHandler(
    ITaskRepository taskRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteTaskCommand, bool>
{
    public async Task<Result<bool>> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<bool>(
                Error.NotFound("Task.NotFound", $"Task with ID '{request.TaskId}' was not found."));
        }

        taskRepository.Remove(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}

public class MoveTaskCommandHandler(
    ITaskRepository taskRepository,
    IBoardColumnRepository columnRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<MoveTaskCommand, TaskDto>
{
    public async Task<Result<TaskDto>> Handle(MoveTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<TaskDto>(
                Error.NotFound("Task.NotFound", $"Task with ID '{request.TaskId}' was not found."));
        }

        var column = await columnRepository.GetByIdAsync(request.TargetColumnId, cancellationToken);
        if (column is null)
        {
            return Result.Failure<TaskDto>(
                Error.NotFound("Column.NotFound", $"Target column with ID '{request.TargetColumnId}' was not found."));
        }

        task.ColumnId = request.TargetColumnId;
        task.Position = request.NewPosition;
        task.Status = request.NewStatus ?? column.TargetStatus;
        task.LastModifiedAtUtc = DateTime.UtcNow;

        taskRepository.Update(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var mapper = new TaskMapper();
        return Result.Success(mapper.TaskToTaskDto(task));
    }
}

public class GetTasksByColumnQueryHandler(
    ITaskRepository taskRepository) : IQueryHandler<GetTasksByColumnQuery, IReadOnlyList<TaskDto>>
{
    public async Task<Result<IReadOnlyList<TaskDto>>> Handle(GetTasksByColumnQuery request, CancellationToken cancellationToken)
    {
        var tasks = await taskRepository.GetTasksByColumnAsync(request.ColumnId, cancellationToken);
        var mapper = new TaskMapper();
        return Result.Success(mapper.TasksToTaskDtos(tasks));
    }
}

public class SearchTasksQueryHandler(
    ITaskRepository taskRepository) : IQueryHandler<SearchTasksQuery, IReadOnlyList<TaskDto>>
{
    public async Task<Result<IReadOnlyList<TaskDto>>> Handle(SearchTasksQuery request, CancellationToken cancellationToken)
    {
        var tasks = await taskRepository.SearchTasksAsync(
            request.ColumnId,
            request.AssigneeId,
            request.Priority,
            request.Status,
            request.LabelId,
            cancellationToken);

        var mapper = new TaskMapper();
        return Result.Success(mapper.TasksToTaskDtos(tasks));
    }
}

public class UpdateTaskStatusCommandHandler(
    ITaskRepository taskRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateTaskStatusCommand, TaskDto>
{
    public async Task<Result<TaskDto>> Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<TaskDto>(
                Error.NotFound("Task.NotFound", $"Task with ID '{request.TaskId}' was not found."));
        }

        task.Status = request.NewStatus;
        task.LastModifiedAtUtc = DateTime.UtcNow;

        taskRepository.Update(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var mapper = new TaskMapper();
        return Result.Success(mapper.TaskToTaskDto(task));
    }
}
