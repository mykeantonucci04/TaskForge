using MediatR;
using TaskForge.Application.Features.Auth;
using TaskForge.Application.Features.Boards;
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

        tasks.MapGet("/column/{columnId:guid}", async (Guid columnId, ISender sender) =>
        {
            var result = await sender.Send(new GetTasksByColumnQuery(columnId));
            return ToHttpResponse(result);
        }).WithName("GetTasksByColumn");

        tasks.MapPatch("/{id:guid}/status", async (Guid id, UpdateTaskStatusCommand command, ISender sender) =>
        {
            var commandWithId = command with { TaskId = id };
            var result = await sender.Send(commandWithId);
            return ToHttpResponse(result);
        }).WithName("UpdateTaskStatus");

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
            ErrorType.NotFound => Microsoft.AspNetCore.Http.Results.NotFound(error),
            ErrorType.Validation => Microsoft.AspNetCore.Http.Results.BadRequest(error),
            ErrorType.Conflict => Microsoft.AspNetCore.Http.Results.Conflict(error),
            ErrorType.Unauthorized => Microsoft.AspNetCore.Http.Results.Unauthorized(),
            ErrorType.Forbidden => Microsoft.AspNetCore.Http.Results.Forbid(),
            _ => Microsoft.AspNetCore.Http.Results.BadRequest(error)
        };
    }
}

public record CreateProjectRequest(string Name, string Key, string? Description);
public record UpdateProjectRequest(string Name, string? Description);
public record CreateBoardRequest(string Name, bool CreateDefaultColumns = true);
public record CreateBoardColumnRequest(string Name, TaskItemStatus TargetStatus = TaskItemStatus.Todo);
