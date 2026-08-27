using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TaskForge.Domain.Interfaces;
using TaskForge.Shared.Constants;
using TaskForge.Shared.Exceptions;

namespace TaskForge.Api.Middleware;

public class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, ITenantRepository tenantRepository)
    {
        // 1. Try reading from JWT Claim
        var tenantIdClaim = context.User?.FindFirstValue(TenantConstants.ClaimName);
        var schemaClaim = context.User?.FindFirstValue(TenantConstants.SchemaClaimName);

        if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var tenantGuid))
        {
            var schema = string.IsNullOrEmpty(schemaClaim) ? $"tenant_{tenantGuid:N}" : schemaClaim;
            tenantContext.SetTenant(tenantGuid, schema);
        }
        // 2. Fallback to Header "X-Tenant-ID"
        else if (context.Request.Headers.TryGetValue(TenantConstants.HeaderName, out var headerValue) &&
                 !string.IsNullOrEmpty(headerValue))
        {
            var tenantIdentifier = headerValue.ToString();
            var tenant = await tenantRepository.GetByIdentifierAsync(tenantIdentifier, context.RequestAborted);
            if (tenant is not null)
            {
                tenantContext.SetTenant(tenant.Id, tenant.SchemaName);
            }
        }

        await next(context);
    }
}

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var problemDetails = exception switch
        {
            ValidationException valEx => new ProblemDetails
            {
                Title = "Validation Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = valEx.Message,
                Extensions = { ["errors"] = valEx.Errors }
            },
            NotFoundException notFoundEx => new ProblemDetails
            {
                Title = "Resource Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = notFoundEx.Message
            },
            UnauthorizedException unauthEx => new ProblemDetails
            {
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = unauthEx.Message
            },
            ForbiddenException forbiddenEx => new ProblemDetails
            {
                Title = "Forbidden",
                Status = StatusCodes.Status403Forbidden,
                Detail = forbiddenEx.Message
            },
            DomainException domainEx => new ProblemDetails
            {
                Title = "Domain Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = domainEx.Message
            },
            _ => new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred. Please try again later."
            }
        };

        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        var json = JsonSerializer.Serialize(problemDetails);
        return context.Response.WriteAsync(json);
    }
}
