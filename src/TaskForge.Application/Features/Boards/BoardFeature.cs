using FluentValidation;
using Riok.Mapperly.Abstractions;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Common.Messaging;
using TaskForge.Application.Features.Tasks;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Enums;
using TaskForge.Domain.Interfaces;
using TaskForge.Shared.Results;

namespace TaskForge.Application.Features.Boards;

public record BoardDto(
    Guid Id,
    Guid TenantId,
    Guid ProjectId,
    string Name,
    DateTime CreatedAtUtc);

public record BoardColumnDto(
    Guid Id,
    Guid BoardId,
    string Name,
    int Order,
    TaskItemStatus TargetStatus);

public record BoardColumnDetailDto(
    Guid Id,
    Guid BoardId,
    string Name,
    int Order,
    TaskItemStatus TargetStatus,
    IReadOnlyList<TaskDto> Tasks);

public record BoardDetailDto(
    Guid Id,
    Guid TenantId,
    Guid ProjectId,
    string Name,
    DateTime CreatedAtUtc,
    IReadOnlyList<BoardColumnDetailDto> Columns);

public record CreateBoardCommand(
    Guid ProjectId,
    string Name,
    bool CreateDefaultColumns = true) : ICommand<BoardDto>;

public record CreateBoardColumnCommand(
    Guid BoardId,
    string Name,
    TaskItemStatus TargetStatus = TaskItemStatus.Todo) : ICommand<BoardColumnDto>;

public record UpdateBoardColumnCommand(
    Guid ColumnId,
    string Name,
    int Order,
    TaskItemStatus TargetStatus) : ICommand<BoardColumnDto>;

public record DeleteBoardColumnCommand(Guid ColumnId) : ICommand<bool>;

public record GetBoardsByProjectQuery(Guid ProjectId) : IQuery<IReadOnlyList<BoardDto>>;

public record GetBoardWithColumnsAndTasksQuery(Guid BoardId) : IQuery<BoardDetailDto>;

[Mapper]
public partial class BoardMapper
{
    public partial BoardDto BoardToBoardDto(Board board);
    public partial IReadOnlyList<BoardDto> BoardsToBoardDtos(IReadOnlyList<Board> boards);
    public partial BoardColumnDto ColumnToColumnDto(BoardColumn column);

    public BoardDetailDto BoardToBoardDetailDto(Board board)
    {
        var taskMapper = new TaskMapper();
        var columns = board.Columns?
            .OrderBy(c => c.Order)
            .Select(c => new BoardColumnDetailDto(
                c.Id,
                c.BoardId,
                c.Name,
                c.Order,
                c.TargetStatus,
                c.Tasks != null ? taskMapper.TasksToTaskDtos(c.Tasks.OrderBy(t => t.Position).ToList()) : Array.Empty<TaskDto>()))
            .ToList() ?? new List<BoardColumnDetailDto>();

        return new BoardDetailDto(
            board.Id,
            board.TenantId,
            board.ProjectId,
            board.Name,
            board.CreatedAtUtc,
            columns);
    }
}

public class CreateBoardCommandValidator : AbstractValidator<CreateBoardCommand>
{
    public CreateBoardCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateBoardColumnCommandValidator : AbstractValidator<CreateBoardColumnCommand>
{
    public CreateBoardColumnCommandValidator()
    {
        RuleFor(x => x.BoardId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TargetStatus).IsInEnum();
    }
}

public class UpdateBoardColumnCommandValidator : AbstractValidator<UpdateBoardColumnCommand>
{
    public UpdateBoardColumnCommandValidator()
    {
        RuleFor(x => x.ColumnId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TargetStatus).IsInEnum();
    }
}

public class CreateBoardCommandHandler(
    IBoardRepository boardRepository,
    IBoardColumnRepository columnRepository,
    ICurrentTenantService tenantService,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateBoardCommand, BoardDto>
{
    public async Task<Result<BoardDto>> Handle(CreateBoardCommand request, CancellationToken cancellationToken)
    {
        if (!tenantService.TenantId.HasValue)
        {
            return Result.Failure<BoardDto>(
                Error.Validation("Tenant.Required", "Tenant context is required to create a board."));
        }

        var tenantId = tenantService.TenantId.Value;
        var board = new Board
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProjectId = request.ProjectId,
            Name = request.Name.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        if (request.CreateDefaultColumns)
        {
            board.Columns.Add(new BoardColumn
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                BoardId = board.Id,
                Name = "To Do",
                Order = 0,
                TargetStatus = TaskItemStatus.Todo
            });

            board.Columns.Add(new BoardColumn
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                BoardId = board.Id,
                Name = "In Progress",
                Order = 1,
                TargetStatus = TaskItemStatus.InProgress
            });

            board.Columns.Add(new BoardColumn
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                BoardId = board.Id,
                Name = "Done",
                Order = 2,
                TargetStatus = TaskItemStatus.Done
            });
        }

        await boardRepository.AddAsync(board, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var mapper = new BoardMapper();
        return Result.Success(mapper.BoardToBoardDto(board));
    }
}

public class GetBoardsByProjectQueryHandler(
    IBoardRepository boardRepository) : IQueryHandler<GetBoardsByProjectQuery, IReadOnlyList<BoardDto>>
{
    public async Task<Result<IReadOnlyList<BoardDto>>> Handle(GetBoardsByProjectQuery request, CancellationToken cancellationToken)
    {
        var boards = await boardRepository.GetBoardsByProjectAsync(request.ProjectId, cancellationToken);
        var mapper = new BoardMapper();
        return Result.Success(mapper.BoardsToBoardDtos(boards));
    }
}

public class GetBoardWithColumnsAndTasksQueryHandler(
    IBoardRepository boardRepository) : IQueryHandler<GetBoardWithColumnsAndTasksQuery, BoardDetailDto>
{
    public async Task<Result<BoardDetailDto>> Handle(GetBoardWithColumnsAndTasksQuery request, CancellationToken cancellationToken)
    {
        var board = await boardRepository.GetBoardWithColumnsAndTasksAsync(request.BoardId, cancellationToken);
        if (board is null)
        {
            return Result.Failure<BoardDetailDto>(
                Error.NotFound("Board.NotFound", $"Board with ID '{request.BoardId}' was not found."));
        }

        var mapper = new BoardMapper();
        return Result.Success(mapper.BoardToBoardDetailDto(board));
    }
}

public class CreateBoardColumnCommandHandler(
    IBoardRepository boardRepository,
    IBoardColumnRepository columnRepository,
    ICurrentTenantService tenantService,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateBoardColumnCommand, BoardColumnDto>
{
    public async Task<Result<BoardColumnDto>> Handle(CreateBoardColumnCommand request, CancellationToken cancellationToken)
    {
        if (!tenantService.TenantId.HasValue)
        {
            return Result.Failure<BoardColumnDto>(
                Error.Validation("Tenant.Required", "Tenant context is required to create a column."));
        }

        var board = await boardRepository.GetByIdAsync(request.BoardId, cancellationToken);
        if (board is null)
        {
            return Result.Failure<BoardColumnDto>(
                Error.NotFound("Board.NotFound", $"Board with ID '{request.BoardId}' was not found."));
        }

        var existingColumns = await columnRepository.GetColumnsByBoardAsync(request.BoardId, cancellationToken);
        var nextOrder = existingColumns.Count > 0 ? existingColumns.Max(c => c.Order) + 1 : 0;

        var column = new BoardColumn
        {
            Id = Guid.NewGuid(),
            TenantId = tenantService.TenantId.Value,
            BoardId = request.BoardId,
            Name = request.Name.Trim(),
            Order = nextOrder,
            TargetStatus = request.TargetStatus
        };

        await columnRepository.AddAsync(column, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var mapper = new BoardMapper();
        return Result.Success(mapper.ColumnToColumnDto(column));
    }
}

public class UpdateBoardColumnCommandHandler(
    IBoardColumnRepository columnRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateBoardColumnCommand, BoardColumnDto>
{
    public async Task<Result<BoardColumnDto>> Handle(UpdateBoardColumnCommand request, CancellationToken cancellationToken)
    {
        var column = await columnRepository.GetByIdAsync(request.ColumnId, cancellationToken);
        if (column is null)
        {
            return Result.Failure<BoardColumnDto>(
                Error.NotFound("Column.NotFound", $"Column with ID '{request.ColumnId}' was not found."));
        }

        column.Name = request.Name.Trim();
        column.Order = request.Order;
        column.TargetStatus = request.TargetStatus;

        columnRepository.Update(column);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var mapper = new BoardMapper();
        return Result.Success(mapper.ColumnToColumnDto(column));
    }
}

public class DeleteBoardColumnCommandHandler(
    IBoardColumnRepository columnRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteBoardColumnCommand, bool>
{
    public async Task<Result<bool>> Handle(DeleteBoardColumnCommand request, CancellationToken cancellationToken)
    {
        var column = await columnRepository.GetByIdAsync(request.ColumnId, cancellationToken);
        if (column is null)
        {
            return Result.Failure<bool>(
                Error.NotFound("Column.NotFound", $"Column with ID '{request.ColumnId}' was not found."));
        }

        columnRepository.Remove(column);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
