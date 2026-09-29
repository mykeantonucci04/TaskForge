using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
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
            ArgumentException argEx => new ProblemDetails
            {
                Title = "Invalid Argument",
                Status = StatusCodes.Status400BadRequest,
                Detail = argEx.Message
            },
            InvalidOperationException invalidOpEx => new ProblemDetails
            {
                Title = "Operation Failed",
                Status = StatusCodes.Status422UnprocessableEntity,
                Detail = invalidOpEx.Message
            },
            DbUpdateException dbEx => CreateDbUpdateProblemDetails(dbEx),
            _ when exception.GetType().FullName?.Contains("Npgsql") == true => new ProblemDetails
            {
                Title = "Database Connection Error",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = "A database connectivity issue occurred. Please try again in a few moments."
            },
            OperationCanceledException => new ProblemDetails
            {
                Title = "Request Cancelled",
                Status = StatusCodes.Status499ClientClosedRequest,
                Detail = "The request was cancelled."
            },
            _ => new ProblemDetails
            {
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred. Please try again later."
            }
        };

        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        var json = JsonSerializer.Serialize(problemDetails, JsonOptions);
        return context.Response.WriteAsync(json);
    }

    private static ProblemDetails CreateDbUpdateProblemDetails(DbUpdateException dbEx)
    {
        var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

        // PostgreSQL unique constraint violation (SQLSTATE 23505)
        if (innerMessage.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
            innerMessage.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
            innerMessage.Contains("23505", StringComparison.Ordinal))
        {
            return new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = "A record with the provided details already exists."
            };
        }

        // PostgreSQL foreign key violation (SQLSTATE 23503)
        if (innerMessage.Contains("foreign key", StringComparison.OrdinalIgnoreCase) ||
            innerMessage.Contains("23503", StringComparison.Ordinal))
        {
            return new ProblemDetails
            {
                Title = "Reference Error",
                Status = StatusCodes.Status400BadRequest,
                Detail = "The operation references a record that does not exist."
            };
        }

        return new ProblemDetails
        {
            Title = "Database Error",
            Status = StatusCodes.Status422UnprocessableEntity,
            Detail = "An error occurred while processing the data. Please verify your input and try again."
        };
    }
}

