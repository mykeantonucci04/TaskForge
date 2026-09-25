using FluentValidation;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Common.Messaging;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Interfaces;
using TaskForge.Shared.Results;

namespace TaskForge.Application.Features.Tasks;

public record AddCommentCommand(Guid TaskId, string Content) : ICommand<CommentDto>;

public record DeleteCommentCommand(Guid CommentId) : ICommand<bool>;

public record GetCommentsByTaskQuery(Guid TaskId) : IQuery<IReadOnlyList<CommentDto>>;

public class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
    }
}

public class AddCommentCommandHandler(
    ITaskRepository taskRepository,
    ICommentRepository commentRepository,
    ICurrentUserService currentUserService,
    ICurrentTenantService tenantService,
    IUnitOfWork unitOfWork) : ICommandHandler<AddCommentCommand, CommentDto>
{
    public async Task<Result<CommentDto>> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        if (!tenantService.TenantId.HasValue)
        {
            return Result.Failure<CommentDto>(
                Error.Validation("Tenant.Required", "Tenant context is required to add a comment."));
        }

        var task = await taskRepository.GetByIdAsync(request.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<CommentDto>(
                Error.NotFound("Task.NotFound", $"Task with ID '{request.TaskId}' was not found."));
        }

        Guid authorId = Guid.Empty;
        if (!string.IsNullOrWhiteSpace(currentUserService.UserId) && Guid.TryParse(currentUserService.UserId, out var parsedAuthorId))
        {
            authorId = parsedAuthorId;
        }

        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantService.TenantId.Value,
            TaskItemId = request.TaskId,
            AuthorId = authorId,
            Content = request.Content.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        await commentRepository.AddAsync(comment, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CommentDto(comment.Id, comment.TaskItemId, comment.AuthorId, comment.Content, comment.CreatedAtUtc));
    }
}

public class DeleteCommentCommandHandler(
    ICommentRepository commentRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteCommentCommand, bool>
{
    public async Task<Result<bool>> Handle(DeleteCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await commentRepository.GetByIdAsync(request.CommentId, cancellationToken);
        if (comment is null)
        {
            return Result.Failure<bool>(
                Error.NotFound("Comment.NotFound", $"Comment with ID '{request.CommentId}' was not found."));
        }

        commentRepository.Remove(comment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}

public class GetCommentsByTaskQueryHandler(
    ICommentRepository commentRepository) : IQueryHandler<GetCommentsByTaskQuery, IReadOnlyList<CommentDto>>
{
    public async Task<Result<IReadOnlyList<CommentDto>>> Handle(GetCommentsByTaskQuery request, CancellationToken cancellationToken)
    {
        var comments = await commentRepository.GetCommentsByTaskAsync(request.TaskId, cancellationToken);
        var dtos = comments.Select(c => new CommentDto(c.Id, c.TaskItemId, c.AuthorId, c.Content, c.CreatedAtUtc)).ToList();
        return Result.Success<IReadOnlyList<CommentDto>>(dtos);
    }
}
