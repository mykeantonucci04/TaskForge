namespace TaskForge.Shared.Constants;

public static class TenantConstants
{
    public const string HeaderName = "X-Tenant-ID";
    public const string ClaimName = "tenant_id";
    public const string SchemaClaimName = "tenant_schema";
    public const string DefaultSchema = "public";
    public const string TenantContextItemKey = "TaskForge_TenantContext";
}

public static class Roles
{
    public const string SystemAdmin = "SystemAdmin";
    public const string TenantAdmin = "TenantAdmin";
    public const string ProjectManager = "ProjectManager";
    public const string Member = "Member";
    public const string Guest = "Guest";
}
