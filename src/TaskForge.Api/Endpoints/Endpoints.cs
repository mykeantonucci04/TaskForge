using MediatR;
using TaskForge.Application.Features.Auth;
using TaskForge.Application.Features.Boards;
using TaskForge.Application.Features.Labels;
using TaskForge.Application.Features.Projects;
using TaskForge.Application.Features.Tasks;
using TaskForge.Application.Features.Tenants;
using TaskForge.Application.Features.Workspaces;
using TaskForge.Domain.Enums;
using TaskForge.Shared.Results;

namespace TaskForge.Api.Endpoints;

public static class EndpointsExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        var apiGroup = app.MapGroup("/api/v1");

        // Auth Endpoints (Public)
        var auth = apiGroup.MapGroup("/auth").WithTags("Authentication");
        
        auth.MapPost("/register-tenant", async (RegisterTenantCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return ToCreatedHttpResponse(result, res => $"/api/v1/tenants/{res.TenantId}");
        }).WithName("RegisterTenant");

        auth.MapPost("/login", async (LoginCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return ToHttpResponse(result);
        }).WithName("Login");

        // Tenant Endpoints
        var tenants = apiGroup.MapGroup("/tenants").WithTags("Tenants");

        tenants.MapPost("/register", async (RegisterTenantCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return ToCreatedHttpResponse(result, res => $"/api/v1/tenants/{res.TenantId}");
        }).WithName("RegisterTenantAlias");

        tenants.MapPost("/", async (CreateTenantCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return ToCreatedHttpResponse(result, res => $"/api/v1/tenants/{res.Id}");
        }).RequireAuthorization().WithName("CreateTenant");

        tenants.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetTenantByIdQuery(id));
            return ToHttpResponse(result);
        }).RequireAuthorization().WithName("GetTenantById");

        // Workspace Endpoints
        var workspaces = apiGroup.MapGroup("/workspaces").WithTags("Workspaces").RequireAuthorization();
        workspaces.MapPost("/", async (CreateWorkspaceCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return ToHttpResponse(result);
        }).WithName("CreateWorkspace");

        workspaces.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new GetWorkspacesQuery());
            return ToHttpResponse(result);
        }).WithName("GetWorkspaces");

        workspaces.MapPost("/{workspaceId:guid}/projects", async (Guid workspaceId, CreateProjectRequest request, ISender sender) =>
        {
            var command = new CreateProjectCommand(workspaceId, request.Name, request.Key, request.Description);
            var result = await sender.Send(command);
            return ToCreatedHttpResponse(result, res => $"/api/v1/projects/{res.Id}");
        }).WithName("CreateProjectInWorkspace");

        workspaces.MapGet("/{workspaceId:guid}/projects", async (Guid workspaceId, ISender sender) =>
        {
            var result = await sender.Send(new GetProjectsByWorkspaceQuery(workspaceId));
            return ToHttpResponse(result);
        }).WithName("GetProjectsByWorkspace");

        workspaces.MapPost("/{workspaceId:guid}/labels", async (Guid workspaceId, CreateLabelRequest request, ISender sender) =>
        {
            var command = new CreateLabelCommand(workspaceId, request.Name, request.ColorHex);
            var result = await sender.Send(command);
            return ToCreatedHttpResponse(result, res => $"/api/v1/workspaces/{workspaceId}/labels");
        }).WithName("CreateLabelInWorkspace");

        workspaces.MapGet("/{workspaceId:guid}/labels", async (Guid workspaceId, ISender sender) =>
        {
            var result = await sender.Send(new GetLabelsByWorkspaceQuery(workspaceId));
            return ToHttpResponse(result);
        }).WithName("GetLabelsByWorkspace");

        // Project Endpoints
        var projects = apiGroup.MapGroup("/projects").WithTags("Projects").RequireAuthorization();
        projects.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetProjectByIdQuery(id));
            return ToHttpResponse(result);
        }).WithName("GetProjectById");

        projects.MapPut("/{id:guid}", async (Guid id, UpdateProjectRequest request, ISender sender) =>
        {
            var command = new UpdateProjectCommand(id, request.Name, request.Description);
            var result = await sender.Send(command);
            return ToHttpResponse(result);
        }).WithName("UpdateProject");

        projects.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteProjectCommand(id));
            return ToHttpResponse(result);
        }).WithName("DeleteProject");

        projects.MapPost("/{projectId:guid}/boards", async (Guid projectId, CreateBoardRequest request, ISender sender) =>
        {
            var command = new CreateBoardCommand(projectId, request.Name, request.CreateDefaultColumns);
            var result = await sender.Send(command);
            return ToCreatedHttpResponse(result, res => $"/api/v1/boards/{res.Id}");
        }).WithName("CreateBoardInProject");

        projects.MapGet("/{projectId:guid}/boards", async (Guid projectId, ISender sender) =>
        {
            var result = await sender.Send(new GetBoardsByProjectQuery(projectId));
            return ToHttpResponse(result);
        }).WithName("GetBoardsByProject");

        // Board & Column Endpoints
        var boards = apiGroup.MapGroup("/boards").WithTags("Boards").RequireAuthorization();
        boards.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetBoardWithColumnsAndTasksQuery(id));
            return ToHttpResponse(result);
        }).WithName("GetBoardDetails");

        boards.MapPost("/{boardId:guid}/columns", async (Guid boardId, CreateBoardColumnRequest request, ISender sender) =>
        {
            var command = new CreateBoardColumnCommand(boardId, request.Name, request.TargetStatus);
            var result = await sender.Send(command);
            return ToHttpResponse(result);
        }).WithName("CreateBoardColumn");

        boards.MapPut("/{boardId:guid}/columns/reorder", async (Guid boardId, ReorderColumnsRequest request, ISender sender) =>
        {
            var command = new ReorderBoardColumnsCommand(boardId, request.Columns);
            var result = await sender.Send(command);
            return ToHttpResponse(result);
        }).WithName("ReorderBoardColumns");

        var columns = apiGroup.MapGroup("/columns").WithTags("Columns").RequireAuthorization();
        columns.MapPut("/{id:guid}", async (Guid id, UpdateBoardColumnCommand command, ISender sender) =>
        {
            var commandWithId = command with { ColumnId = id };
            var result = await sender.Send(commandWithId);
            return ToHttpResponse(result);
        }).WithName("UpdateBoardColumn");

        columns.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteBoardColumnCommand(id));
            return ToHttpResponse(result);
        }).WithName("DeleteBoardColumn");

        // Task Endpoints
        var tasks = apiGroup.MapGroup("/tasks").WithTags("Tasks").RequireAuthorization();
        tasks.MapPost("/", async (CreateTaskCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return ToHttpResponse(result);
        }).WithName("CreateTask");

        tasks.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetTaskByIdQuery(id));
            return ToHttpResponse(result);
        }).WithName("GetTaskById");

        tasks.MapPut("/{id:guid}", async (Guid id, UpdateTaskRequest request, ISender sender) =>
        {
            var command = new UpdateTaskCommand(id, request.Title, request.Description, request.Priority, request.AssigneeId, request.DueDateUtc);
            var result = await sender.Send(command);
            return ToHttpResponse(result);
        }).WithName("UpdateTask");

        tasks.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteTaskCommand(id));
            return ToHttpResponse(result);
        }).WithName("DeleteTask");

        tasks.MapPost("/{id:guid}/move", async (Guid id, MoveTaskRequest request, ISender sender) =>
        {
            var command = new MoveTaskCommand(id, request.TargetColumnId, request.NewPosition, request.NewStatus);
            var result = await sender.Send(command);
            return ToHttpResponse(result);
        }).WithName("MoveTask");

        tasks.MapGet("/column/{columnId:guid}", async (Guid columnId, ISender sender) =>
        {
            var result = await sender.Send(new GetTasksByColumnQuery(columnId));
            return ToHttpResponse(result);
        }).WithName("GetTasksByColumn");

        tasks.MapGet("/search", async (
            Guid? columnId,
            Guid? assigneeId,
            TaskPriority? priority,
            TaskItemStatus? status,
            Guid? labelId,
            ISender sender) =>
        {
            var result = await sender.Send(new SearchTasksQuery(columnId, assigneeId, priority, status, labelId));
            return ToHttpResponse(result);
        }).WithName("SearchTasks");

        tasks.MapPatch("/{id:guid}/status", async (Guid id, UpdateTaskStatusCommand command, ISender sender) =>
        {
            var commandWithId = command with { TaskId = id };
            var result = await sender.Send(commandWithId);
            return ToHttpResponse(result);
        }).WithName("UpdateTaskStatus");

        tasks.MapPost("/{id:guid}/subtasks", async (Guid id, AddSubtaskRequest request, ISender sender) =>
        {
            var command = new AddSubtaskCommand(id, request.Title);
            var result = await sender.Send(command);
            return ToHttpResponse(result);
        }).WithName("AddSubtask");

        tasks.MapGet("/{id:guid}/subtasks", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetSubtasksByTaskQuery(id));
            return ToHttpResponse(result);
        }).WithName("GetSubtasksByTask");

        tasks.MapPost("/{id:guid}/comments", async (Guid id, AddCommentRequest request, ISender sender) =>
        {
            var command = new AddCommentCommand(id, request.Content);
            var result = await sender.Send(command);
            return ToHttpResponse(result);
        }).WithName("AddComment");

        tasks.MapGet("/{id:guid}/comments", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetCommentsByTaskQuery(id));
            return ToHttpResponse(result);
        }).WithName("GetCommentsByTask");

        tasks.MapPost("/{id:guid}/labels/{labelId:guid}", async (Guid id, Guid labelId, ISender sender) =>
        {
            var result = await sender.Send(new AssignLabelToTaskCommand(id, labelId));
            return ToHttpResponse(result);
        }).WithName("AssignLabelToTask");

        tasks.MapDelete("/{id:guid}/labels/{labelId:guid}", async (Guid id, Guid labelId, ISender sender) =>
        {
            var result = await sender.Send(new RemoveLabelFromTaskCommand(id, labelId));
            return ToHttpResponse(result);
        }).WithName("RemoveLabelFromTask");

        // Subtask Endpoints
        var subtasks = apiGroup.MapGroup("/subtasks").WithTags("Subtasks").RequireAuthorization();
        subtasks.MapPatch("/{id:guid}/toggle", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new ToggleSubtaskCommand(id));
            return ToHttpResponse(result);
        }).WithName("ToggleSubtask");

        subtasks.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteSubtaskCommand(id));
            return ToHttpResponse(result);
        }).WithName("DeleteSubtask");

        // Comment Endpoints
        var comments = apiGroup.MapGroup("/comments").WithTags("Comments").RequireAuthorization();
        comments.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteCommentCommand(id));
            return ToHttpResponse(result);
        }).WithName("DeleteComment");

        return app;
    }

    private static IResult ToHttpResponse<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Microsoft.AspNetCore.Http.Results.Ok(result.Value);
        }

        return MapErrorResult(result.Error);
    }

    private static IResult ToCreatedHttpResponse<T>(Result<T> result, Func<T, string> uriSelector)
    {
        if (result.IsSuccess)
        {
            var uri = uriSelector(result.Value);
            return Microsoft.AspNetCore.Http.Results.Created(uri, result.Value);
        }

        return MapErrorResult(result.Error);
    }

    private static IResult MapErrorResult(Error error)
    {
        return error.Type switch
        {
            ErrorType.NotFound => Microsoft.AspNetCore.Http.Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: error.Description),
            ErrorType.Validation => Microsoft.AspNetCore.Http.Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error",
                detail: error.Description),
            ErrorType.Conflict => Microsoft.AspNetCore.Http.Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: error.Description),
            ErrorType.Unauthorized => Microsoft.AspNetCore.Http.Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: string.IsNullOrWhiteSpace(error.Description)
                    ? "Credenziali non valide o email non registrata."
                    : error.Description),
            ErrorType.Forbidden => Microsoft.AspNetCore.Http.Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: error.Description),
            _ => Microsoft.AspNetCore.Http.Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: error.Description)
        };
    }
}

public record CreateProjectRequest(string Name, string Key, string? Description);
public record UpdateProjectRequest(string Name, string? Description);
public record CreateBoardRequest(string Name, bool CreateDefaultColumns = true);
public record CreateBoardColumnRequest(string Name, TaskItemStatus TargetStatus = TaskItemStatus.Todo);
public record ReorderColumnsRequest(IReadOnlyList<ColumnOrderDto> Columns);
public record CreateLabelRequest(string Name, string ColorHex);
public record UpdateTaskRequest(string Title, string? Description, TaskPriority Priority, Guid? AssigneeId, DateTime? DueDateUtc);
public record MoveTaskRequest(Guid TargetColumnId, double NewPosition, TaskItemStatus? NewStatus = null);
public record AddSubtaskRequest(string Title);
public record AddCommentRequest(string Content);
