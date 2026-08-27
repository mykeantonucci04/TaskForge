using TaskForge.Domain.Common;
using TaskForge.Domain.Enums;

namespace TaskForge.Domain.Events;

public record TaskCreatedEvent(Guid TaskId, Guid TenantId, string Title, Guid ColumnId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public record TaskStatusChangedEvent(Guid TaskId, Guid TenantId, TaskItemStatus OldStatus, TaskItemStatus NewStatus) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}

public record TenantCreatedEvent(Guid TenantId, string Name, string SchemaName) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
