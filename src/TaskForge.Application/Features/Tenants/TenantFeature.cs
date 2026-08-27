using FluentValidation;
using Riok.Mapperly.Abstractions;
using TaskForge.Application.Common.Messaging;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Interfaces;

namespace TaskForge.Application.Features.Tenants;

public record TenantDto(Guid Id, string Name, string SchemaName, string Identifier, bool IsActive, DateTime CreatedAtUtc);

public record CreateTenantCommand(string Name, string Identifier) : ICommand<TenantDto>;

public record GetTenantByIdQuery(Guid Id) : IQuery<TenantDto>;

[Mapper]
public partial class TenantMapper
{
    public partial TenantDto TenantToTenantDto(Tenant tenant);
}

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Identifier).NotEmpty().MaximumLength(50).Matches("^[a-z0-9-]+$");
    }
}

public class CreateTenantCommandHandler(
    ITenantRepository tenantRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateTenantCommand, TenantDto>
{
    public async Task<Shared.Results.Result<TenantDto>> Handle(CreateTenantCommand request, CancellationToken cancellationToken)
    {
        var existing = await tenantRepository.GetByIdentifierAsync(request.Identifier, cancellationToken);
        if (existing is not null)
        {
            return Shared.Results.Result.Failure<TenantDto>(
                Shared.Results.Error.Conflict("Tenant.Exists", $"Tenant with identifier '{request.Identifier}' already exists."));
        }

        var schemaName = $"tenant_{Guid.NewGuid():N}";
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Identifier = request.Identifier,
            SchemaName = schemaName,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        await tenantRepository.AddAsync(tenant, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var mapper = new TenantMapper();
        return mapper.TenantToTenantDto(tenant);
    }
}

public class GetTenantByIdQueryHandler(ITenantRepository tenantRepository) : IQueryHandler<GetTenantByIdQuery, TenantDto>
{
    public async Task<Shared.Results.Result<TenantDto>> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
        {
            return Shared.Results.Result.Failure<TenantDto>(
                Shared.Results.Error.NotFound("Tenant.NotFound", $"Tenant '{request.Id}' was not found."));
        }

        var mapper = new TenantMapper();
        return mapper.TenantToTenantDto(tenant);
    }
}
