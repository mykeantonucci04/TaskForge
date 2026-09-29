using FluentAssertions;
using NSubstitute;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Features.Boards;
using TaskForge.Application.Features.Projects;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Enums;
using TaskForge.Domain.Interfaces;
using Xunit;

namespace TaskForge.Application.UnitTests;

public class ProjectBoardUnitTests
{
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IBoardRepository _boardRepository = Substitute.For<IBoardRepository>();
    private readonly IBoardColumnRepository _columnRepository = Substitute.For<IBoardColumnRepository>();
    private readonly ICurrentTenantService _tenantService = Substitute.For<ICurrentTenantService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public ProjectBoardUnitTests()
    {
        _tenantService.TenantId.Returns(Guid.NewGuid());
    }

    // ==========================================
    // Projects CQRS Tests
    // ==========================================

    [Fact]
    public async Task CreateProjectCommandHandler_Should_Create_Project_When_Valid()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var command = new CreateProjectCommand(workspaceId, "Mobile App", "MOB", "Mobile application project");

        _projectRepository.ExistsByKeyAsync(workspaceId, "MOB", Arg.Any<CancellationToken>())
            .Returns(false);

        var handler = new CreateProjectCommandHandler(_projectRepository, _tenantService, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Mobile App");
        result.Value.Key.Should().Be("MOB");
        result.Value.WorkspaceId.Should().Be(workspaceId);
        result.Value.TenantId.Should().Be(_tenantService.TenantId!.Value);

        await _projectRepository.Received(1).AddAsync(Arg.Is<Project>(p => p.Key == "MOB" && p.Name == "Mobile App"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProjectCommandHandler_Should_Return_Conflict_When_Key_Already_Exists()
    {
        // Arrange
        var workspaceId = Guid.NewGuid();
        var command = new CreateProjectCommand(workspaceId, "Mobile App", "MOB", null);

        _projectRepository.ExistsByKeyAsync(workspaceId, "MOB", Arg.Any<CancellationToken>())
            .Returns(true);

        var handler = new CreateProjectCommandHandler(_projectRepository, _tenantService, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Project.KeyExists");
        await _projectRepository.DidNotReceive().AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProjectCommandHandler_Should_Return_Validation_Error_When_Tenant_Context_Missing()
    {
        // Arrange
        _tenantService.TenantId.Returns((Guid?)null);
        var command = new CreateProjectCommand(Guid.NewGuid(), "Backend", "BE", null);
        var handler = new CreateProjectCommandHandler(_projectRepository, _tenantService, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.Required");
    }

    // ==========================================
    // Boards & Columns CQRS Tests
    // ==========================================

    [Fact]
    public async Task CreateBoardCommandHandler_Should_Create_Board_With_Default_Columns()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var command = new CreateBoardCommand(projectId, "Sprint 1 Board", CreateDefaultColumns: true);
        var handler = new CreateBoardCommandHandler(_boardRepository, _columnRepository, _tenantService, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Sprint 1 Board");
        result.Value.ProjectId.Should().Be(projectId);

        await _boardRepository.Received(1).AddAsync(
            Arg.Is<Board>(b => b.Name == "Sprint 1 Board" && b.Columns.Count == 3),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBoardCommandHandler_Should_Create_Empty_Board_When_Flag_Is_False()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var command = new CreateBoardCommand(projectId, "Blank Board", CreateDefaultColumns: false);
        var handler = new CreateBoardCommandHandler(_boardRepository, _columnRepository, _tenantService, _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _boardRepository.Received(1).AddAsync(
            Arg.Is<Board>(b => b.Name == "Blank Board" && b.Columns.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBoardWithColumnsAndTasksQueryHandler_Should_Return_Board_Details()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var board = new Board
        {
            Id = boardId,
            TenantId = _tenantService.TenantId!.Value,
            ProjectId = Guid.NewGuid(),
            Name = "Kanban Main",
            CreatedAtUtc = DateTime.UtcNow
        };

        var column = new BoardColumn
        {
            Id = Guid.NewGuid(),
            BoardId = boardId,
            TenantId = board.TenantId,
            Name = "In Progress",
            Order = 1,
            TargetStatus = TaskItemStatus.InProgress
        };
        column.Tasks.Add(new TaskItem
        {
            Id = Guid.NewGuid(),
            ColumnId = column.Id,
            TenantId = board.TenantId,
            Title = "Implement Auth",
            Position = 1000.0,
            Priority = TaskPriority.High,
            Status = TaskItemStatus.InProgress,
            CreatedAtUtc = DateTime.UtcNow
        });
        board.Columns.Add(column);

        _boardRepository.GetBoardWithColumnsAndTasksAsync(boardId, Arg.Any<CancellationToken>())
            .Returns(board);

        var handler = new GetBoardWithColumnsAndTasksQueryHandler(_boardRepository);

        // Act
        var result = await handler.Handle(new GetBoardWithColumnsAndTasksQuery(boardId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Kanban Main");
        result.Value.Columns.Should().HaveCount(1);
        result.Value.Columns[0].Name.Should().Be("In Progress");
        result.Value.Columns[0].Tasks.Should().HaveCount(1);
        result.Value.Columns[0].Tasks[0].Title.Should().Be("Implement Auth");
    }

    [Fact]
    public async Task CreateBoardColumnCommandHandler_Should_Calculate_Next_Order_Correctly()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var existingColumns = new List<BoardColumn>
        {
            new() { Id = Guid.NewGuid(), BoardId = boardId, Order = 0, Name = "Col 1" },
            new() { Id = Guid.NewGuid(), BoardId = boardId, Order = 2, Name = "Col 2" }
        };

        _boardRepository.GetByIdAsync(boardId, Arg.Any<CancellationToken>())
            .Returns(new Board { Id = boardId, Name = "Test Board" });

        _columnRepository.GetColumnsByBoardAsync(boardId, Arg.Any<CancellationToken>())
            .Returns(existingColumns);

        var handler = new CreateBoardColumnCommandHandler(_boardRepository, _columnRepository, _tenantService, _unitOfWork);
        var command = new CreateBoardColumnCommand(boardId, "QA Testing", TaskItemStatus.InProgress);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("QA Testing");
        result.Value.Order.Should().Be(3); // Max(0, 2) + 1 = 3

        await _columnRepository.Received(1).AddAsync(
            Arg.Is<BoardColumn>(c => c.Name == "QA Testing" && c.Order == 3 && c.BoardId == boardId),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReorderBoardColumnsCommandHandler_Should_Reorder_Columns_Successfully()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var col1Id = Guid.NewGuid();
        var col2Id = Guid.NewGuid();
        var col3Id = Guid.NewGuid();

        var col1 = new BoardColumn { Id = col1Id, BoardId = boardId, Name = "Todo", Order = 0 };
        var col2 = new BoardColumn { Id = col2Id, BoardId = boardId, Name = "In Progress", Order = 1 };
        var col3 = new BoardColumn { Id = col3Id, BoardId = boardId, Name = "Done", Order = 2 };

        var existingColumns = new List<BoardColumn> { col1, col2, col3 };

        var tenantId = _tenantService.TenantId!.Value;
        _boardRepository.GetByIdAsync(boardId, Arg.Any<CancellationToken>())
            .Returns(new Board { Id = boardId, TenantId = tenantId, Name = "Test Board" });

        _columnRepository.GetColumnsByBoardAsync(boardId, Arg.Any<CancellationToken>())
            .Returns(existingColumns);

        var handler = new ReorderBoardColumnsCommandHandler(_boardRepository, _columnRepository, _tenantService, _unitOfWork);
        var command = new ReorderBoardColumnsCommand(boardId, new List<ColumnOrderDto>
        {
            new(col1Id, 2),
            new(col2Id, 0),
            new(col3Id, 1)
        });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        col1.Order.Should().Be(2);
        col2.Order.Should().Be(0);
        col3.Order.Should().Be(1);

        _columnRepository.Received(1).Update(col1);
        _columnRepository.Received(1).Update(col2);
        _columnRepository.Received(1).Update(col3);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReorderBoardColumnsCommandHandler_Should_Return_NotFound_When_Board_Does_Not_Exist()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        _boardRepository.GetByIdAsync(boardId, Arg.Any<CancellationToken>())
            .Returns((Board?)null);

        var handler = new ReorderBoardColumnsCommandHandler(_boardRepository, _columnRepository, _tenantService, _unitOfWork);
        var command = new ReorderBoardColumnsCommand(boardId, new List<ColumnOrderDto>
        {
            new(Guid.NewGuid(), 0)
        });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Board.NotFound");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReorderBoardColumnsCommandHandler_Should_Return_NotFound_When_Column_Does_Not_Belong_To_Board()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        var existingColId = Guid.NewGuid();
        var foreignColId = Guid.NewGuid();

        var existingCol = new BoardColumn { Id = existingColId, BoardId = boardId, Name = "Todo", Order = 0 };

        var tenantId = _tenantService.TenantId!.Value;
        _boardRepository.GetByIdAsync(boardId, Arg.Any<CancellationToken>())
            .Returns(new Board { Id = boardId, TenantId = tenantId, Name = "Test Board" });

        _columnRepository.GetColumnsByBoardAsync(boardId, Arg.Any<CancellationToken>())
            .Returns(new List<BoardColumn> { existingCol });

        var handler = new ReorderBoardColumnsCommandHandler(_boardRepository, _columnRepository, _tenantService, _unitOfWork);
        var command = new ReorderBoardColumnsCommand(boardId, new List<ColumnOrderDto>
        {
            new(existingColId, 0),
            new(foreignColId, 1)
        });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Column.NotFound");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReorderBoardColumnsCommandHandler_Should_Return_Validation_Error_When_Tenant_Context_Missing()
    {
        // Arrange
        _tenantService.TenantId.Returns((Guid?)null);
        var handler = new ReorderBoardColumnsCommandHandler(_boardRepository, _columnRepository, _tenantService, _unitOfWork);
        var command = new ReorderBoardColumnsCommand(Guid.NewGuid(), new List<ColumnOrderDto>
        {
            new(Guid.NewGuid(), 0)
        });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Tenant.Required");
    }

    [Fact]
    public void ReorderBoardColumnsCommandValidator_Should_Pass_When_Command_Is_Valid()
    {
        // Arrange
        var validator = new ReorderBoardColumnsCommandValidator();
        var command = new ReorderBoardColumnsCommand(Guid.NewGuid(), new List<ColumnOrderDto>
        {
            new(Guid.NewGuid(), 0),
            new(Guid.NewGuid(), 1),
            new(Guid.NewGuid(), 2)
        });

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ReorderBoardColumnsCommandValidator_Should_Fail_When_BoardId_Is_Empty()
    {
        // Arrange
        var validator = new ReorderBoardColumnsCommandValidator();
        var command = new ReorderBoardColumnsCommand(Guid.Empty, new List<ColumnOrderDto>
        {
            new(Guid.NewGuid(), 0)
        });

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ReorderBoardColumnsCommand.BoardId));
    }

    [Fact]
    public void ReorderBoardColumnsCommandValidator_Should_Fail_When_Columns_Is_Empty()
    {
        // Arrange
        var validator = new ReorderBoardColumnsCommandValidator();
        var command = new ReorderBoardColumnsCommand(Guid.NewGuid(), Array.Empty<ColumnOrderDto>());

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ReorderBoardColumnsCommand.Columns));
    }

    [Fact]
    public void ReorderBoardColumnsCommandValidator_Should_Fail_When_Column_Has_Empty_Id_Or_Negative_Order()
    {
        // Arrange
        var validator = new ReorderBoardColumnsCommandValidator();
        var command = new ReorderBoardColumnsCommand(Guid.NewGuid(), new List<ColumnOrderDto>
        {
            new(Guid.Empty, -1)
        });

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.EndsWith(nameof(ColumnOrderDto.ColumnId)));
        result.Errors.Should().Contain(e => e.PropertyName.EndsWith(nameof(ColumnOrderDto.Order)));
    }

    [Fact]
    public void ReorderBoardColumnsCommandValidator_Should_Fail_When_ColumnIds_Have_Duplicates()
    {
        // Arrange
        var validator = new ReorderBoardColumnsCommandValidator();
        var duplicateColId = Guid.NewGuid();
        var command = new ReorderBoardColumnsCommand(Guid.NewGuid(), new List<ColumnOrderDto>
        {
            new(duplicateColId, 0),
            new(duplicateColId, 1)
        });

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("unique", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ReorderBoardColumnsCommandValidator_Should_Fail_When_Orders_Have_Duplicates()
    {
        // Arrange
        var validator = new ReorderBoardColumnsCommandValidator();
        var command = new ReorderBoardColumnsCommand(Guid.NewGuid(), new List<ColumnOrderDto>
        {
            new(Guid.NewGuid(), 0),
            new(Guid.NewGuid(), 0)
        });

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("unique", StringComparison.OrdinalIgnoreCase));
    }
}
