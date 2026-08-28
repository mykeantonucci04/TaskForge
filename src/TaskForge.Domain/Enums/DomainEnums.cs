namespace TaskForge.Domain.Enums;

public enum TenantPlan
{
    Free = 1,
    Pro = 2,
    Enterprise = 3
}

public enum TenantStatus
{
    Active = 1,
    Suspended = 2,
    Cancelled = 3
}

public enum MembershipRole
{
    Owner = 1,
    Admin = 2,
    Member = 3,
    Guest = 4
}

public enum MembershipStatus
{
    Pending = 1,
    Active = 2,
    Removed = 3
}

public enum TaskPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4
}

public enum TaskItemStatus
{
    Backlog = 1,
    Todo = 2,
    InProgress = 3,
    InReview = 4,
    Done = 5
}

public enum UserRole
{
    SystemAdmin = 1,
    TenantAdmin = 2,
    ProjectManager = 3,
    Member = 4,
    Guest = 5
}

public enum ActivityType
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    StatusChanged = 4,
    CommentAdded = 5,
    AttachmentAdded = 6
}
