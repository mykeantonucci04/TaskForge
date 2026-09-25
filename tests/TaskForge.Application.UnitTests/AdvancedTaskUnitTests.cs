using FluentAssertions;
using NSubstitute;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Features.Labels;
using TaskForge.Application.Features.Tasks;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Enums;
using TaskForge.Domain.Interfaces;
using Xunit;

namespace TaskForge.Application.UnitTests;

public class AdvancedTaskUnitTests
{
    private readonly ITaskRepository _taskRepository = Substitute.For<ITaskRepository>();
    private readonly IBoardColumnRepository _columnRepository = Substitute.For<IBoardColumnRepository>();
    private readonly ISubtaskRepository _subtaskRepository = Substitute.For<ISubtaskRepository>();
    private readonly ICommentRepository _commentRepository = Substitute.For<ICommentRepository>();
    private readonly ILabelRepository _labelRepository = Substitute.For<ILabelRepository>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly ICurrentTenantService _tenantService = Substitute.For<ICurrentTenantService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public AdvancedTaskUnitTests()
    {
        _tenantService.TenantId.Returns(Guid.NewGuid());
    }

    // ==========================================
    // MoveTaskCommand Tests (Drag & Drop)
    // ==========================================

    [Fact]
    public async Task MoveTaskCommandHandler_Should_Update_Column_Position_And_Adopt_Target_Status()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var sourceColumnId = Guid.NewGuid();
        var targetColumnId = Guid.NewGuid();

        var task = new TaskItem
        {
            Id = taskId,
            ColumnId = sourceColumnId,
            Position = 1000.0,
            Status = TaskItemStatus.Todo,
            Title = "Task to move"
        };

        var targetColumn = new BoardColumn
        {
            Id = targetColumnId,
            Name = "In Progress",
            TargetStatus = TaskItemStatus.InProgress
        };

        _taskRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>()).Returns(task);
        _columnRepository.GetByIdAsync(targetColumnId, Arg.Any<CancellationToken>()).Returns(targetColumn);

        var handler = new MoveTaskCommandHandler(_taskRepository, _columnRepository, _unitOfWork);
        var command = new MoveTaskCommand(taskId, targetColumnId, 500.0);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ColumnId.Should().Be(targetColumnId);
        result.Value.Position.Should().Be(500.0);
        result.Value.Status.Should().Be(TaskItemStatus.InProgress);

        _taskRepository.Received(1).Update(Arg.Is<TaskItem>(t =>
            t.ColumnId == targetColumnId &&
            t.Position == 500.0 &&
            t.Status == TaskItemStatus.InProgress));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveTaskCommandHandler_Should_Respect_Explicit_Status_When_Provided()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var targetColumnId = Guid.NewGuid();

        var task = new TaskItem
        {
            Id = taskId,
            ColumnId = Guid.NewGuid(),
            Position = 1000.0,
            Status = TaskItemStatus.Todo,
            Title = "Blocked task"
        };

        var targetColumn = new BoardColumn
        {
            Id = targetColumnId,
            Name = "Waiting",
            TargetStatus = TaskItemStatus.InProgress
        };

        _taskRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>()).Returns(task);
        _columnRepository.GetByIdAsync(targetColumnId, Arg.Any<CancellationToken>()).Returns(targetColumn);

        var handler = new MoveTaskCommandHandler(_taskRepository, _columnRepository, _unitOfWork);
        var command = new MoveTaskCommand(taskId, targetColumnId, 250.0, TaskItemStatus.InReview);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(TaskItemStatus.InReview);
    }

    // ==========================================
    // Subtask Tests
    // ==========================================

    [Fact]
    public async Task AddSubtaskCommandHandler_Should_Create_Subtask_Uncompleted()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new TaskItem { Id = taskId, Title = "Parent Task" };

        _taskRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>()).Returns(task);

        var handler = new AddSubtaskCommandHandler(_taskRepository, _subtaskRepository, _tenantService, _unitOfWork);
        var command = new AddSubtaskCommand(taskId, "Write unit tests");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Write unit tests");
        result.Value.IsCompleted.Should().BeFalse();
        result.Value.TaskItemId.Should().Be(taskId);

        await _subtaskRepository.Received(1).AddAsync(Arg.Is<Subtask>(s =>
            s.Title == "Write unit tests" &&
            !s.IsCompleted &&
            s.TaskItemId == taskId), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleSubtaskCommandHandler_Should_Flip_Completion_State()
    {
        // Arrange
        var subtaskId = Guid.NewGuid();
        var subtask = new Subtask { Id = subtaskId, Title = "Deploy to prod", IsCompleted = false };

        _subtaskRepository.GetByIdAsync(subtaskId, Arg.Any<CancellationToken>()).Returns(subtask);

        var handler = new ToggleSubtaskCommandHandler(_subtaskRepository, _unitOfWork);
        var command = new ToggleSubtaskCommand(subtaskId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsCompleted.Should().BeTrue();
        _subtaskRepository.Received(1).Update(Arg.Is<Subtask>(s => s.IsCompleted));
    }

    // ==========================================
    // Comment Tests
    // ==========================================

    [Fact]
    public async Task AddCommentCommandHandler_Should_Track_Author_And_Save()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var authorGuid = Guid.NewGuid();
        var task = new TaskItem { Id = taskId, Title = "Discuss architecture" };

        _currentUserService.UserId.Returns(authorGuid.ToString());
        _taskRepository.GetByIdAsync(taskId, Arg.Any<CancellationToken>()).Returns(task);

        var handler = new AddCommentCommandHandler(_taskRepository, _commentRepository, _currentUserService, _tenantService, _unitOfWork);
        var command = new AddCommentCommand(taskId, "LGTM! Approved for deploy.");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().Be("LGTM! Approved for deploy.");
        result.Value.AuthorId.Should().Be(authorGuid);
        result.Value.TaskItemId.Should().Be(taskId);

        await _commentRepository.Received(1).AddAsync(Arg.Is<Comment>(c =>
            c.Content == "LGTM! Approved for deploy." &&
            c.AuthorId == authorGuid), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ==========================================
    // Label Tests
    // ==========================================

    [Fact]
    public async Task CreateLabelCommandHandler_Should_Create_Label_When_Valid()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var command = new CreateLabelCommand(workspaceId, "Bug", "#E74C3C");

        _labelRepository.ExistsByNameAsync(workspaceId, "Bug", Arg.Any<CancellationToken>()).Returns(false);

        var handler = new CreateLabelCommandHandler(_labelRepository, _tenantService, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Bug");
        result.Value.ColorHex.Should().Be("#E74C3C");

        await _labelRepository.Received(1).AddAsync(Arg.Is<Label>(l =>
            l.Name == "Bug" &&
            l.ColorHex == "#E74C3C"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignLabelToTaskCommandHandler_Should_Add_Label_To_Task_Collection()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var labelId = Guid.NewGuid();

        var task = new TaskItem { Id = taskId, Title = "Fix issue" };
        var label = new Label { Id = labelId, Name = "Frontend", ColorHex = "#3498DB" };

        _taskRepository.GetTaskWithDetailsAsync(taskId, Arg.Any<CancellationToken>()).Returns(task);
        _labelRepository.GetByIdAsync(labelId, Arg.Any<CancellationToken>()).Returns(label);

        var handler = new AssignLabelToTaskCommandHandler(_taskRepository, _labelRepository, _unitOfWork);
        var command = new AssignLabelToTaskCommand(taskId, labelId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        task.Labels.Should().Contain(label);
        _taskRepository.Received(1).Update(task);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
