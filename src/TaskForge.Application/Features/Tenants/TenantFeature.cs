using FluentValidation;
using TaskForge.Application.Common.Messaging;
using TaskForge.Domain.Common;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Enums;
using TaskForge.Domain.Interfaces;
using TaskForge.Shared.Results;

namespace TaskForge.Application.Features.Tenants;

public record TenantDto(
    Guid Id,
    string Name,
    string SchemaName,
    string Slug,
    TenantPlan Plan,
    TenantStatus Status,
    DateTime CreatedAtUtc);

public record CreateTenantCommand(
    string Name,
    string Slug,
    TenantPlan Plan = TenantPlan.Free) : ICommand<TenantDto>;

public record GetTenantByIdQuery(Guid Id) : IQuery<TenantDto>;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Slug).NotEmpty().MinimumLength(3).MaximumLength(50).Matches("^[a-z0-9-]+$");
    }
}

public class CreateTenantCommandHandler(
    ITenantRepository tenantRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateTenantCommand, TenantDto>
{
    public async Task<Result<TenantDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var slugVo = TenantSlug.Create(request.Slug);

        var existing = await tenantRepository.ExistsBySlugAsync(slugVo, cancellationToken);
        if (existing)
        {
            return Result.Failure<TenantDto>(
                Error.Conflict("Tenant.Exists", $"Tenant with identifier '{request.Slug}' already exists."));
        }

        var tenant = Tenant.Create(request.Name, slugVo, request.Plan);

        await tenantRepository.AddAsync(tenant, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new TenantDto(
            tenant.Id,
            tenant.Name,
            tenant.SchemaName,
            tenant.Slug.Value,
            tenant.Plan,
            tenant.Status,
            tenant.CreatedAtUtc);

        return Result.Success(dto);
    }
}

public class GetTenantByIdQueryHandler(ITenantRepository tenantRepository) : IQueryHandler<GetTenantByIdQuery, TenantDto>
{
    public async Task<Result<TenantDto>> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
        {
            return Result.Failure<TenantDto>(
                Error.NotFound("Tenant.NotFound", $"Tenant '{request.Id}' was not found."));
        }

        var dto = new TenantDto(
            tenant.Id,
            tenant.Name,
            tenant.SchemaName,
            tenant.Slug.Value,
            tenant.Plan,
            tenant.Status,
            tenant.CreatedAtUtc);

        return Result.Success(dto);
    }
}
