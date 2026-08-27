using FluentValidation;
using Riok.Mapperly.Abstractions;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Common.Messaging;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Enums;
using TaskForge.Domain.Interfaces;

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

public record CreateTaskCommand(
    Guid ColumnId,
    string Title,
    string? Description,
    TaskPriority Priority,
    Guid? AssigneeId,
    DateTime? DueDateUtc) : ICommand<TaskDto>;

public record GetTasksByColumnQuery(Guid ColumnId) : IQuery<IReadOnlyList<TaskDto>>;

public record UpdateTaskStatusCommand(Guid TaskId, TaskItemStatus NewStatus) : ICommand<TaskDto>;

[Mapper]
public partial class TaskMapper
{
    public partial TaskDto TaskToTaskDto(TaskItem taskItem);
    public partial IReadOnlyList<TaskDto> TasksToTaskDtos(IReadOnlyList<TaskItem> taskItems);
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

public class CreateTaskCommandHandler(
    ITaskRepository taskRepository,
    ICurrentTenantService tenantService,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateTaskCommand, TaskDto>
{
    public async Task<Shared.Results.Result<TaskDto>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        if (!tenantService.TenantId.HasValue)
        {
            return Shared.Results.Result.Failure<TaskDto>(
                Shared.Results.Error.Validation("Tenant.Required", "Tenant context is required to create a task."));
        }

        var taskItem = new TaskItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantService.TenantId.Value,
            ColumnId = request.ColumnId,
            AssigneeId = request.AssigneeId,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Status = TaskItemStatus.Todo,
            Position = 1000.0,
            DueDateUtc = request.DueDateUtc,
            CreatedAtUtc = DateTime.UtcNow
        };

        await taskRepository.AddAsync(taskItem, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var mapper = new TaskMapper();
        return mapper.TaskToTaskDto(taskItem);
    }
}

public class GetTasksByColumnQueryHandler(
    ITaskRepository taskRepository) : IQueryHandler<GetTasksByColumnQuery, IReadOnlyList<TaskDto>>
{
    public async Task<Shared.Results.Result<IReadOnlyList<TaskDto>>> Handle(GetTasksByColumnQuery request, CancellationToken cancellationToken)
    {
        var tasks = await taskRepository.GetTasksByColumnAsync(request.ColumnId, cancellationToken);
        var mapper = new TaskMapper();
        return Shared.Results.Result.Success(mapper.TasksToTaskDtos(tasks));
    }
}

public class UpdateTaskStatusCommandHandler(
    ITaskRepository taskRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateTaskStatusCommand, TaskDto>
{
    public async Task<Shared.Results.Result<TaskDto>> Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Shared.Results.Result.Failure<TaskDto>(
                Shared.Results.Error.NotFound("Task.NotFound", $"Task '{request.TaskId}' was not found."));
        }

        task.Status = request.NewStatus;
        task.LastModifiedAtUtc = DateTime.UtcNow;

        taskRepository.Update(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var mapper = new TaskMapper();
        return mapper.TaskToTaskDto(task);
    }
}
