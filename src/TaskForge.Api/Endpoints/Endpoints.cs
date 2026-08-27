using MediatR;
using TaskForge.Application.Features.Auth;
using TaskForge.Application.Features.Tasks;
using TaskForge.Application.Features.Tenants;
using TaskForge.Application.Features.Workspaces;
using TaskForge.Shared.Results;

namespace TaskForge.Api.Endpoints;

public static class EndpointsExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        var apiGroup = app.MapGroup("/api/v1");

        // Auth Endpoints
        var auth = apiGroup.MapGroup("/auth").WithTags("Authentication");
        auth.MapPost("/login", async (LoginCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return ToHttpResponse(result);
        }).WithName("Login");

        // Tenant Endpoints
        var tenants = apiGroup.MapGroup("/tenants").WithTags("Tenants").RequireAuthorization();
        tenants.MapPost("/", async (CreateTenantCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return ToHttpResponse(result);
        }).WithName("CreateTenant");

        tenants.MapGet("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetTenantByIdQuery(id));
            return ToHttpResponse(result);
        }).WithName("GetTenantById");

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

        return result.Error.Type switch
        {
            ErrorType.NotFound => Microsoft.AspNetCore.Http.Results.NotFound(result.Error),
            ErrorType.Validation => Microsoft.AspNetCore.Http.Results.BadRequest(result.Error),
            ErrorType.Conflict => Microsoft.AspNetCore.Http.Results.Conflict(result.Error),
            ErrorType.Unauthorized => Microsoft.AspNetCore.Http.Results.Unauthorized(),
            ErrorType.Forbidden => Microsoft.AspNetCore.Http.Results.Forbid(),
            _ => Microsoft.AspNetCore.Http.Results.BadRequest(result.Error)
        };
    }
}
