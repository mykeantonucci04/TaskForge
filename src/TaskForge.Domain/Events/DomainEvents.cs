using TaskForge.Domain.Common;
using TaskForge.Domain.Enums;

namespace TaskForge.Domain.Events;

public record TenantCreatedEvent(
    Guid TenantId,
    string Name,
    string Slug,
    string SchemaName,
    TenantPlan Plan) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public record TaskCreatedEvent(
    Guid TaskId,
    Guid TenantId,
    string Title,
    Guid ColumnId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public record TaskStatusChangedEvent(
    Guid TaskId,
    Guid TenantId,
    TaskItemStatus OldStatus,
    TaskItemStatus NewStatus) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
